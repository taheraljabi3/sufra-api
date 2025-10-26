using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Couriers;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ حماية كل الكنترولر بالتوكن
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🚴‍♂️ Couriers API")]
    public class CouriersController : ControllerBase
    {
        private readonly ICourierService _courierService;
        private readonly ILogger<CouriersController> _logger;

        public CouriersController(ICourierService courierService, ILogger<CouriersController> logger)
        {
            _courierService = courierService;
            _logger = logger;
        }

        // ============================================================
        /// <summary>📋 جلب جميع المندوبين (للأدمن والمالك فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _courierService.GetAllAsync();

                if (result == null || !result.Any())
                    return Ok(new { message = "ℹ️ لا يوجد مندوبون حالياً.", couriers = Array.Empty<object>() });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} مندوباً.",
                    couriers = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب جميع المندوبين");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المندوبين", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>🔍 جلب مندوب معين عبر المعرّف</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner,courier")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _courierService.GetByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = $"❌ المندوب رقم {id} غير موجود." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب المندوب {Id}", id);
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات المندوب", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>📍 جلب المندوبين حسب المنطقة (للأدمن والمالك فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpGet("zone/{zoneId:int}")]
        public async Task<IActionResult> GetByZone(int zoneId)
        {
            try
            {
                var result = await _courierService.GetByZoneAsync(zoneId);

                if (result == null || !result.Any())
                    return Ok(new { message = "ℹ️ لا يوجد مندوبون في هذه المنطقة.", couriers = Array.Empty<object>() });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} مندوباً في المنطقة {zoneId}.",
                    couriers = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب المندوبين في المنطقة {ZoneId}", zoneId);
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المندوبين في المنطقة", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>➕ إنشاء مندوب جديد (للمالك فقط)</summary>
        // ============================================================
        [Authorize(Roles = "owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourierDto dto)
        {
            try
            {
                var result = await _courierService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, new
                {
                    message = "✅ تم إنشاء المندوب بنجاح.",
                    courier = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في إنشاء المندوب الجديد");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء المندوب", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>🔄 تحديث حالة المندوب (نشط / غير نشط) — للأدمن أو المالك فقط</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            try
            {
                var success = await _courierService.UpdateStatusAsync(id, status);

                if (!success)
                    return NotFound(new { message = $"❌ المندوب رقم {id} غير موجود." });

                return Ok(new { message = $"✅ تم تحديث حالة المندوب إلى {status}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تحديث حالة المندوب {Id}", id);
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الحالة", details = ex.Message });
            }
        }
    }
}
