using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.Interfaces;
using Sufra.Application.DTOs.Zones;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ حماية جميع المسارات بالـ JWT
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🗺️ Zones API")]
    public class ZonesController : ControllerBase
    {
        private readonly IZoneService _zoneService;
        private readonly ILogger<ZonesController> _logger;

        public ZonesController(IZoneService zoneService, ILogger<ZonesController> logger)
        {
            _zoneService = zoneService;
            _logger = logger;
        }

        // ============================================================
        /// <summary>📍 جلب جميع المناطق (مفتوح للطلاب والمندوبين)</summary>
        // ============================================================
        [AllowAnonymous] // 👈 يمكن لأي مستخدم أو تطبيق الوصول
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zones = await _zoneService.GetAllAsync();

            if (zones == null || zones.Count == 0)
                return NotFound(new { message = "❌ لا توجد مناطق حالياً." });

            return Ok(new
            {
                message = $"✅ تم جلب {zones.Count} منطقة بنجاح.",
                data = zones
            });
        }

        // ============================================================
        /// <summary>📍 جلب منطقة محددة بالـ Id</summary>
        // ============================================================
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var zone = await _zoneService.GetByIdAsync(id);
            if (zone == null)
                return NotFound(new { message = $"❌ المنطقة رقم {id} غير موجودة." });

            return Ok(new
            {
                message = "✅ تم جلب المنطقة بنجاح.",
                data = zone
            });
        }

        // ============================================================
        /// <summary>➕ إضافة منطقة جديدة (للمشرفين فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateZoneDto dto)
        {
            try
            {
                var zone = await _zoneService.CreateAsync(dto);
                _logger.LogInformation("✅ تم إنشاء منطقة جديدة {Name}", zone.Name);

                return CreatedAtAction(nameof(GetById), new { id = zone.Id }, new
                {
                    message = "✅ تم إنشاء المنطقة بنجاح.",
                    data = zone
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في إنشاء المنطقة الجديدة");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء المنطقة.", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>✏️ تحديث بيانات منطقة (للمشرفين فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateZoneDto dto)
        {
            try
            {
                var updated = await _zoneService.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { message = "❌ المنطقة غير موجودة." });

                return Ok(new { message = "✅ تم تحديث المنطقة بنجاح.", data = updated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تحديث المنطقة {Id}", id);
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث المنطقة.", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>🗑️ حذف منطقة (للمشرفين فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _zoneService.DeleteAsync(id);
                if (!success)
                    return NotFound(new { message = "❌ المنطقة غير موجودة." });

                _logger.LogInformation("🗑️ تم حذف المنطقة {Id}", id);
                return Ok(new { message = "✅ تم حذف المنطقة بنجاح." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في حذف المنطقة {Id}", id);
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف المنطقة.", details = ex.Message });
            }
        }
    }
}
