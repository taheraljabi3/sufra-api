using System.Collections.Generic;
using System.Threading.Tasks;
using Sufra.Application.DTOs.Zones;

namespace Sufra.Application.Interfaces
{
    public interface IZoneService
    {
        Task<List<ZoneDto>> GetAllAsync();
        Task<ZoneDto?> GetByIdAsync(int id);
    }
}
