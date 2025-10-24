using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Zones;
using Sufra.Application.Interfaces;
using Sufra.Infrastructure.Persistence;

namespace Sufra.Application.Services
{
    public class ZoneService : IZoneService
    {
        private readonly SufraDbContext _context;

        public ZoneService(SufraDbContext context)
        {
            _context = context;
        }

        public async Task<List<ZoneDto>> GetAllAsync()
        {
            return await _context.Zones
                .Select(z => new ZoneDto
                {
                    Id = z.Id,
                    Name = z.Name,
                    Type = z.Type,
                    ReferenceCode = z.ReferenceCode,
                    Status = z.Status,
                    CreatedAt = z.CreatedAt
                })
                .OrderBy(z => z.Id)
                .ToListAsync();
        }

        public async Task<ZoneDto?> GetByIdAsync(int id)
        {
            var zone = await _context.Zones.FindAsync(id);
            if (zone == null) return null;

            return new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                Type = zone.Type,
                ReferenceCode = zone.ReferenceCode,
                Status = zone.Status,
                CreatedAt = zone.CreatedAt
            };
        }
    }
}
