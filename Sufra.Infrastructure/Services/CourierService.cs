using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Couriers;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;

namespace Sufra.Infrastructure.Services
{
    public class CourierService : ICourierService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public CourierService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ✅ جلب جميع المندوبين
        public async Task<IEnumerable<CourierDto>> GetAllAsync()
        {
            var couriers = await _context.Couriers
                .Include(c => c.Zone)
                .OrderBy(c => c.Status)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourierDto>>(couriers);
        }

        // ✅ جلب مندوب بالمعرف
        public async Task<CourierDto?> GetByIdAsync(int id)
        {
            var courier = await _context.Couriers
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == id);

            return _mapper.Map<CourierDto?>(courier);
        }

        // ✅ جلب المندوبين ضمن منطقة معينة
        public async Task<IEnumerable<CourierDto>> GetByZoneAsync(int zoneId)
        {
            var couriers = await _context.Couriers
                .Where(c => c.ZoneId == zoneId)
                .Include(c => c.Zone)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourierDto>>(couriers);
        }

        // ✅ إنشاء مندوب جديد
        public async Task<CourierDto> CreateAsync(CreateCourierDto dto)
        {
            var courier = _mapper.Map<Courier>(dto);
            courier.JoinedAt = DateTime.UtcNow;
            courier.Status = "active";

            _context.Couriers.Add(courier);
            await _context.SaveChangesAsync();

            return _mapper.Map<CourierDto>(courier);
        }

        // ✅ تحديث حالة المندوب (نشط / غير نشط)
        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var courier = await _context.Couriers.FindAsync(id);
            if (courier == null) return false;

            courier.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ جلب أول مندوب متاح في المنطقة (للتعيين التلقائي)
       // ✅ جلب أول مندوب متاح في المنطقة (للتعيين التلقائي)
        public async Task<CourierDto?> GetAvailableByZoneAsync(int zoneId)
        {
            var courier = await _context.Couriers
                .Where(c => c.ZoneId == zoneId && c.Status == "نشط")
                .OrderBy(c => c.JoinedAt)
                .FirstOrDefaultAsync();

            return _mapper.Map<CourierDto?>(courier);
        }

    }
}
