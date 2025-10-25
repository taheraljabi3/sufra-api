using System.Collections.Generic;
using System.Threading.Tasks;
using Sufra.Application.DTOs.Zones;


namespace Sufra.Application.Interfaces
{
    public interface IZoneService
    {
        Task<List<ZoneDto>> GetAllAsync();
        Task<ZoneDto?> GetByIdAsync(int id);
        Task<ZoneDto?> UpdateAsync(int id, UpdateZoneDto dto);
        Task<bool> DeleteAsync(int id);
        Task<ZoneDto> CreateAsync(CreateZoneDto dto);

        // ✅ دوال التعديل والحذف (كي لا تتكرر الأخطاء لاحقًا)
    }
    
}
