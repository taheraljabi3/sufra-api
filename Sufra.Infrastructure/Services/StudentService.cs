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
        /// 📋 جلب جميع الطلاب بترتيب الأحدث أولًا.
        /// </summary>
        public async Task<IEnumerable<StudentDto>> GetAllAsync()
        {
            var students = await _context.Students
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        // =====================================================================
        /// <summary>
        /// 🔍 جلب طالب عبر المعرف الداخلي (Id).
        /// </summary>
        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            return student == null ? null : _mapper.Map<StudentDto>(student);
        }

        // =====================================================================
        /// <summary>
        /// ➕ إنشاء طالب جديد مع تشفير كلمة المرور.
        /// </summary>
        public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
        {
            // 🔍 تحقق إذا الرقم الجامعي مستخدم مسبقًا
            bool exists = await _context.Students
                .AnyAsync(s => s.UniversityId == dto.UniversityId);

            if (exists)
                throw new InvalidOperationException("الرقم الجامعي مسجّل مسبقًا.");

            // ✅ تشفير كلمة المرور قبل الحفظ
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 🧱 إنشاء الكيان
            var student = _mapper.Map<Student>(dto);
            student.Password = hashedPassword;
            student.CreatedAt = DateTime.UtcNow;
            student.Status = "active";
            student.Role = "student";

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return _mapper.Map<StudentDto>(student);
        }

        // =====================================================================
        /// <summary>
        /// ✏️ تحديث بيانات الطالب.
        /// </summary>
        public async Task<StudentDto?> UpdateAsync(int id, UpdateStudentDto dto)
        {
            var entity = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return null;

            // ✅ تحديث الحقول الموجودة فقط
            if (!string.IsNullOrWhiteSpace(dto.UniversityId)) entity.UniversityId = dto.UniversityId!;
            if (!string.IsNullOrWhiteSpace(dto.Name))         entity.Name         = dto.Name!;
            if (!string.IsNullOrWhiteSpace(dto.Email))        entity.Email        = dto.Email!;
            if (!string.IsNullOrWhiteSpace(dto.Phone))        entity.Phone        = dto.Phone!;
            if (!string.IsNullOrWhiteSpace(dto.Status))       entity.Status       = dto.Status!;
            if (!string.IsNullOrWhiteSpace(dto.Password))
                entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password); // 🔒 إعادة تعيين كلمة مرور مشفّرة

            await _context.SaveChangesAsync();
            return _mapper.Map<StudentDto>(entity);
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
            await _context.SaveChangesAsync();
            return true;
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

            return new StudentDto
            {
                Id = student.Id,
                UniversityId = student.UniversityId,
                Name = student.Name,
                Email = student.Email,
                Phone = student.Phone,
                Status = student.Status,
                Role = student.Role

            };
        }
    }
}
