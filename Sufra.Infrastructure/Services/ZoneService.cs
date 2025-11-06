using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Zones;
using Sufra.Application.Interfaces;
using Sufra.Infrastructure.Persistence;
using AutoMapper;

namespace Sufra.Application.Services
{
    public class ZoneService : IZoneService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public ZoneService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ✅ جلب جميع المناطق
        public async Task<List<ZoneDto>> GetAllAsync()
        {
            var zones = await _context.Zones
                .AsNoTracking()
                .OrderBy(z => z.Id)
                .ToListAsync();

            return _mapper.Map<List<ZoneDto>>(zones);
        }

        // ✅ جلب منطقة بالمعرف
        public async Task<ZoneDto?> GetByIdAsync(int id)
        {
            var zone = await _context.Zones
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Id == id);

            return zone == null ? null : _mapper.Map<ZoneDto>(zone);
        }

        // ✅ إنشاء منطقة جديدة
        public async Task<ZoneDto> CreateAsync(CreateZoneDto dto)
        {
            var entity = _mapper.Map<Domain.Entities.Zone>(dto);
            entity.Status = string.IsNullOrWhiteSpace(entity.Status) ? "active" : entity.Status;
            entity.CreatedAt = DateTime.UtcNow;

            _context.Zones.Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<ZoneDto>(entity);
        }

        // ✅ تحديث بيانات المنطقة
        public async Task<ZoneDto?> UpdateAsync(int id, UpdateZoneDto dto)
        {
            var zone = await _context.Zones.FindAsync(id);
            if (zone == null) return null;

            zone.Name = dto.Name ?? zone.Name;
            zone.Type = dto.Type ?? zone.Type;
            zone.ReferenceCode = dto.ReferenceCode ?? zone.ReferenceCode;
            zone.Status = dto.Status ?? zone.Status;
            zone.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<ZoneDto>(zone);
        }

        // ✅ حذف منطقة
        public async Task<bool> DeleteAsync(int id)
        {
            var zone = await _context.Zones.FindAsync(id);
            if (zone == null) return false;

            _context.Zones.Remove(zone);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
