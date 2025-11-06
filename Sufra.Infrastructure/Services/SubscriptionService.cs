using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;

namespace Sufra.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public SubscriptionService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ============================================================
        // 📋 جلب جميع الاشتراكات (مع بيانات الطالب)
        // ============================================================
        public async Task<IEnumerable<SubscriptionDto>> GetAllAsync()
        {
            var subs = await _context.Subscriptions
                .Include(s => s.Student)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return subs.Select(s => new SubscriptionDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student?.Name ?? "",
                UniversityId = s.Student?.UniversityId ?? "",
                PlanCode = s.PlanCode,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status
            });
        }

        // ============================================================
        // 🔍 جلب اشتراك محدد بالمعرّف
        // ============================================================
        public async Task<SubscriptionDto?> GetByIdAsync(int id)
        {
            var s = await _context.Subscriptions
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return null;

            return new SubscriptionDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student?.Name ?? "",
                UniversityId = s.Student?.UniversityId ?? "",
                PlanCode = s.PlanCode,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status
            };
        }

        // ============================================================
        // 🟢 جلب الاشتراك النشط لطالب معين
        // ============================================================
        public async Task<SubscriptionDto?> GetActiveByStudentAsync(int studentId)
        {
            var s = await _context.Subscriptions
                .Include(x => x.Student)
                .Where(x => x.StudentId == studentId && x.Status == "active")
                .FirstOrDefaultAsync();

            if (s == null) return null;

            return new SubscriptionDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student?.Name ?? "",
                UniversityId = s.Student?.UniversityId ?? "",
                PlanCode = s.PlanCode,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status
            };
        }

        // ============================================================
        // ➕ إنشاء اشتراك جديد
        // ============================================================
        public async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto)
        {
            // 🔎 تحقق من عدم وجود اشتراك نشط للطالب
            var hasActive = await _context.Subscriptions
                .AnyAsync(s => s.StudentId == dto.StudentId && s.Status == "active");

            if (hasActive)
                throw new InvalidOperationException("⚠️ هذا الطالب لديه اشتراك نشط بالفعل.");

            var sub = _mapper.Map<Subscription>(dto);
            sub.StartDate = dto.StartDate ?? DateTime.UtcNow;
            sub.EndDate = dto.EndDate ?? sub.StartDate.AddMonths(1);
            sub.Status = "active";
            sub.CreatedAt = DateTime.UtcNow;

            _context.Subscriptions.Add(sub);
            await _context.SaveChangesAsync();

            // إعادة الجلب مع بيانات الطالب
            var created = await _context.Subscriptions
                .Include(s => s.Student)
                .FirstAsync(s => s.Id == sub.Id);

            return new SubscriptionDto
            {
                Id = created.Id,
                StudentId = created.StudentId,
                StudentName = created.Student?.Name ?? "",
                UniversityId = created.Student?.UniversityId ?? "",
                PlanCode = created.PlanCode,
                StartDate = created.StartDate,
                EndDate = created.EndDate,
                Status = created.Status
            };
        }

        // ============================================================
        // ✏️ تحديث اشتراك
        // ============================================================
        public async Task<SubscriptionDto?> UpdateAsync(int id, UpdateSubscriptionDto dto)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.PlanCode))
                sub.PlanCode = dto.PlanCode;
            if (dto.StartDate.HasValue)
                sub.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue)
                sub.EndDate = dto.EndDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.Status))
                sub.Status = dto.Status;

            await _context.SaveChangesAsync();

            var updated = await _context.Subscriptions
                .Include(s => s.Student)
                .FirstAsync(s => s.Id == sub.Id);

            return new SubscriptionDto
            {
                Id = updated.Id,
                StudentId = updated.StudentId,
                StudentName = updated.Student?.Name ?? "",
                UniversityId = updated.Student?.UniversityId ?? "",
                PlanCode = updated.PlanCode,
                StartDate = updated.StartDate,
                EndDate = updated.EndDate,
                Status = updated.Status
            };
        }

        // ============================================================
        // ❌ إلغاء اشتراك
        // ============================================================
        public async Task<bool> CancelAsync(int id)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null) return false;

            sub.Status = "cancelled";
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
