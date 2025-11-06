using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Students;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;
using BCrypt.Net; // ✅ لتشفير كلمات المرور

namespace Sufra.Infrastructure.Services
{
    public class StudentService : IStudentService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public StudentService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // =====================================================================
        /// <summary>
        /// 📋 جلب جميع الطلاب مع بيانات السكن الحالية.
        /// </summary>
        public async Task<IEnumerable<StudentDto>> GetAllAsync()
        {
            var students = await _context.Students
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // 🧱 جلب بيانات السكن الحالية مرة واحدة
            var housings = await _context.StudentHousings
                .Include(h => h.Zone)
                .Where(h => h.IsCurrent)
                .ToListAsync();

            var result = students.Select(s =>
            {
                var housing = housings.FirstOrDefault(h => h.StudentId == s.Id);

                var dto = _mapper.Map<StudentDto>(s);
                dto.ZoneId = housing?.ZoneId; // ✅ أضف هذا السطر
                dto.ZoneName = housing?.Zone?.Name ?? "—";
                dto.RoomNo = housing?.RoomNo ?? "—";
                return dto;
            }).ToList();

            return result;
        }

        // =====================================================================
        /// <summary>
        /// 🔍 جلب طالب عبر المعرف الداخلي (Id) مع بيانات السكن الحالية.
        /// </summary>
        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return null;

            var dto = _mapper.Map<StudentDto>(student);

            // 🧩 جلب السكن الحالي لهذا الطالب
            var housing = await _context.StudentHousings
                .Include(h => h.Zone)
                .FirstOrDefaultAsync(h => h.StudentId == id && h.IsCurrent);

            dto.ZoneName = housing?.Zone?.Name ?? "—";
            dto.RoomNo = housing?.RoomNo ?? "—";

            return dto;
        }

public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
{
    // 🔍 تحقق إذا الرقم الجامعي مستخدم مسبقًا
    bool exists = await _context.Students
        .AnyAsync(s => s.UniversityId == dto.UniversityId);

    if (exists)
        throw new InvalidOperationException("❌ الرقم الجامعي مسجّل مسبقًا.");

    // ✅ تشفير كلمة المرور قبل الحفظ
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    // 🧱 إنشاء كيان الطالب
    var student = _mapper.Map<Student>(dto);
    student.Password = hashedPassword;
    student.CreatedAt = DateTime.UtcNow;
    student.Status = "active";

    // ⚙️ تحديد الدور
    if (string.IsNullOrWhiteSpace(dto.Role))
        student.Role = "student";
    else
    {
        var role = dto.Role.ToLower();
        if (role == "owner")
            throw new InvalidOperationException("🚫 لا يمكن إنشاء مستخدم بدور 'owner' من هذه الواجهة.");
        student.Role = role;
    }

    // 🧩 حفظ الطالب أولاً للحصول على StudentId
    _context.Students.Add(student);
    await _context.SaveChangesAsync();

    // 🏠 إضافة سجل السكن (اختياري)
    if (dto.ZoneId.HasValue && !string.IsNullOrWhiteSpace(dto.RoomNo))
    {
        // ✅ تأكد أن المنطقة موجودة فعلاً قبل الحفظ
        var zoneExists = await _context.Zones.AnyAsync(z => z.Id == dto.ZoneId.Value);
        if (!zoneExists)
            throw new InvalidOperationException("❌ المنطقة المحددة غير موجودة.");

        var housing = new StudentHousing
        {
            StudentId = student.Id,
            ZoneId = dto.ZoneId.Value,
            RoomNo = dto.RoomNo,
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StudentHousings.Add(housing);
        await _context.SaveChangesAsync();
    }

    // 🔁 تجهيز النتيجة النهائية مع بيانات السكن (إن وجدت)
    var result = _mapper.Map<StudentDto>(student);

    if (dto.ZoneId.HasValue)
    {
        var zone = await _context.Zones.FindAsync(dto.ZoneId.Value);
        result.ZoneName = zone?.Name ?? "—";
        result.RoomNo = dto.RoomNo ?? "—";
    }

    return result;
}
// =====================================================================
/// <summary>
/// ✏️ تحديث بيانات الطالب (يُسمح بتعديل الدور فقط إن تم تمريره من Owner).
/// </summary>
public async Task<StudentDto?> UpdateAsync(int id, UpdateStudentDto dto)
{
    var entity = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
    if (entity == null) return null;

    // ✅ تحديث الحقول الأساسية
    if (!string.IsNullOrWhiteSpace(dto.UniversityId))
        entity.UniversityId = dto.UniversityId!;
    if (!string.IsNullOrWhiteSpace(dto.Name))
        entity.Name = dto.Name!;
    if (!string.IsNullOrWhiteSpace(dto.Email))
        entity.Email = dto.Email!;
    if (!string.IsNullOrWhiteSpace(dto.Phone))
        entity.Phone = dto.Phone!;
    if (!string.IsNullOrWhiteSpace(dto.Status))
        entity.Status = dto.Status!;
    if (!string.IsNullOrWhiteSpace(dto.Password))
        entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    // 🧩 تحديث الدور (لو تم تمريره)
    if (!string.IsNullOrWhiteSpace(dto.Role))
    {
        var role = dto.Role.ToLower();
        if (role == "owner")
            throw new InvalidOperationException("🚫 لا يمكن تعيين مستخدم بدور 'owner'.");
        entity.Role = role;
    }

    // ✅ حفظ تحديثات الطالب
    await _context.SaveChangesAsync();

    // =====================================================================
    // 🏠 تحديث بيانات السكن (إن تم تمريرها)
    // =====================================================================
    if (dto.ZoneId.HasValue || !string.IsNullOrWhiteSpace(dto.RoomNo))
    {
        var housing = await _context.StudentHousings
            .FirstOrDefaultAsync(h => h.StudentId == id && h.IsCurrent);

        if (housing != null)
        {
            // تعديل السكن الحالي
            if (dto.ZoneId.HasValue)
                housing.ZoneId = dto.ZoneId.Value;
            if (!string.IsNullOrWhiteSpace(dto.RoomNo))
                housing.RoomNo = dto.RoomNo;
            housing.CreatedAt = DateTime.UtcNow;
        }
        else if (dto.ZoneId.HasValue && !string.IsNullOrWhiteSpace(dto.RoomNo))
        {
            // إنشاء سجل جديد للسكن إن لم يكن موجودًا
            _context.StudentHousings.Add(new StudentHousing
            {
                StudentId = id,
                ZoneId = dto.ZoneId.Value,
                RoomNo = dto.RoomNo,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    // =====================================================================
    // 🎯 تجهيز النتيجة النهائية مع بيانات السكن
    // =====================================================================
    var result = _mapper.Map<StudentDto>(entity);

    var currentHousing = await _context.StudentHousings
        .Include(h => h.Zone)
        .FirstOrDefaultAsync(h => h.StudentId == id && h.IsCurrent);

    if (currentHousing != null)
    {
        result.ZoneName = currentHousing.Zone?.Name ?? "—";
        result.RoomNo = currentHousing.RoomNo ?? "—";
    }

    return result;
}

        // =====================================================================
        /// <summary>
        /// 🗑️ حذف طالب عبر المعرف الداخلي.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"⚠️ Delete failed for student {id}: {ex.InnerException?.Message ?? ex.Message}");
                throw new Exception("❌ لا يمكن حذف الطالب لارتباطه بسجلات أخرى في النظام.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected delete error: {ex.Message}");
                throw new Exception("⚠️ حدث خطأ غير متوقع أثناء حذف الطالب.");
            }
        }
// =====================================================================
/// <summary>
/// 🔍 جلب طالب عبر الرقم الجامعي (UniversityId).
/// </summary>
public async Task<StudentDto?> GetByUniversityIdAsync(string universityId)
{
    var student = await _context.Students
        .FirstOrDefaultAsync(s => s.UniversityId == universityId);

    if (student == null) return null;

    var dto = _mapper.Map<StudentDto>(student);

    // 🏘️ جلب بيانات السكن والمنطقة
    var housing = await _context.StudentHousings
        .Include(h => h.Zone)
        .FirstOrDefaultAsync(h => h.StudentId == student.Id && h.IsCurrent);

    dto.ZoneId = housing?.ZoneId; // ✅ أضف هذا السطر
    dto.ZoneName = housing?.Zone?.Name ?? "—";
    dto.RoomNo = housing?.RoomNo ?? "—";

    // 🚴‍♂️ جلب CourierId في حال كان الطالب مندوبًا
    if (student.Role != null && student.Role.ToLower() == "courier")
    {
        var courier = await _context.Couriers
            .FirstOrDefaultAsync(c => c.StudentId == student.Id);

        if (courier != null)
            dto.CourierId = courier.Id;
    }

    return dto;
}

    }
}
