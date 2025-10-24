using Sufra.Application.DTOs.StudentHousing;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Sufra.Application.Services
{
    public class StudentHousingService : IStudentHousingService
    {
        private readonly SufraDbContext _context;

        public StudentHousingService(SufraDbContext context)
        {
            _context = context;
        }

        // ✅ إضافة أو تحديث موقع الطالب الحالي
        public async Task<bool> UpsertHousingAsync(StudentHousingDto dto)
        {
            var existing = await _context.StudentHousings
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.IsCurrent);

            if (existing != null)
            {
                // 🔄 تحديث السجل الحالي
                existing.HousingUnitId = dto.HousingUnitId;
                existing.RoomNo = dto.RoomNo;
                existing.ZoneId = dto.ZoneId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // 🆕 إنشاء سجل جديد
                var housing = new StudentHousing
                {
                    StudentId = dto.StudentId,
                    HousingUnitId = dto.HousingUnitId,
                    RoomNo = dto.RoomNo,
                    ZoneId = dto.ZoneId,
                    IsCurrent = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StudentHousings.Add(housing);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ جلب الموقع الحالي للطالب
        public async Task<StudentHousing?> GetCurrentAsync(int studentId)
        {
            return await _context.StudentHousings
                .Include(x => x.Zone)
                .FirstOrDefaultAsync(x => x.StudentId == studentId && x.IsCurrent);
        }
    }
}
