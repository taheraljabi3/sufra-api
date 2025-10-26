using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Deliveries;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ حماية كل الكنترولر بالتوكن
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🚚 Deliveries API")]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<DeliveriesController> _logger;

        public DeliveriesController(IDeliveryService deliveryService, ILogger<DeliveriesController> logger)
        {
            _deliveryService = deliveryService;
            _logger = logger;
        }

        // ============================================================
        /// <summary>🚴‍♂️ جلب جميع المهام الخاصة بالمندوب الحالي (من التوكن)</summary>
        // ============================================================
        [Authorize(Roles = "Courier,Admin,Owner")]
        [HttpGet("courier")]
        public async Task<IActionResult> GetByCurrentCourier()
        {
            try
            {
                // 🧭 جلب رقم المندوب من التوكن
                var courierIdClaim = User.FindFirst("UserId")?.Value;
                var role = User.FindFirst("Role")?.Value ?? "Courier";

                if (courierIdClaim == null)
                    return Unauthorized(new { message = "❌ لا يمكن تحديد هوية المندوب من التوكن." });

                int courierId = int.Parse(courierIdClaim);

                var result = await _deliveryService.GetByCourierAsync(courierId);

                if (result == null || !result.Any())
                    return Ok(new
                    {
                        message = "ℹ️ لا توجد مهام حالياً لهذا المندوب.",
                        tasks = Array.Empty<object>()
                    });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} مهمة نشطة بنجاح.",
                    tasks = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب مهام المندوب الحالي");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المهام", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>📦 تأكيد تسليم الطلب من قبل المندوب</summary>
        // ============================================================
        [Authorize(Roles = "Courier,Admin,Owner")]
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] CreateDeliveryProofDto dto)
        {
            try
            {
                // يمكننا أيضًا التحقق من أن المستخدم الحالي هو نفس المندوب
                var courierIdClaim = User.FindFirst("UserId")?.Value;
                if (courierIdClaim == null)
                    return Unauthorized(new { message = "❌ لا يمكن تحديد هوية المندوب من التوكن." });

                int courierId = int.Parse(courierIdClaim);
                dto.CourierId = courierId; // ✅ ضمان التوافق مع المندوب الحالي

                var result = await _deliveryService.ConfirmDeliveryAsync(dto);

                return Ok(new
                {
                    message = "✅ تم تأكيد تسليم الطلب بنجاح.",
                    delivery = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تأكيد التسليم للطلب {MealRequestId}", dto.MealRequestId);
                return StatusCode(500, new { message = "حدث خطأ أثناء تأكيد التسليم", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>🧭 (للأدمن أو المالك) جلب جميع عمليات التوصيل</summary>
        // ============================================================
        [Authorize(Roles = "Admin,Owner")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _deliveryService.GetAllAsync();

                if (result == null || !result.Any())
                    return Ok(new
                    {
                        message = "ℹ️ لا توجد عمليات توصيل مسجلة حالياً.",
                        deliveries = Array.Empty<object>()
                    });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} عملية توصيل.",
                    deliveries = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب جميع بيانات التوصيل");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات التوصيل", details = ex.Message });
            }
        }
    }
}
