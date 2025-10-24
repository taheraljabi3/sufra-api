using Microsoft.AspNetCore.Mvc;
using Sufra.Application.Interfaces;
using Sufra.Application.DTOs.Zones;
using System.Threading.Tasks;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZonesController : ControllerBase
    {
        private readonly IZoneService _zoneService;

        public ZonesController(IZoneService zoneService)
        {
            _zoneService = zoneService;
        }

        // ✅ جلب جميع المناطق (الوحدات)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zones = await _zoneService.GetAllAsync();
            if (zones == null || zones.Count == 0)
                return NotFound(new { message = "❌ لا توجد مناطق حالياً." });

            return Ok(zones);
        }

        // ✅ جلب منطقة محددة بالـ Id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var zone = await _zoneService.GetByIdAsync(id);
            if (zone == null)
                return NotFound(new { message = $"❌ المنطقة رقم {id} غير موجودة." });

            return Ok(zone);
        }
    }
}
