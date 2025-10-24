using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Deliveries;
using Sufra.Application.Interfaces;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        // 🚴‍♂️ 1️⃣ جلب جميع المهام الخاصة بمندوب معين
        // ============================================================
        [HttpGet("courier/{courierId}")]
        public async Task<IActionResult> GetByCourier(int courierId)
        {
            try
            {
                var result = await _deliveryService.GetByCourierAsync(courierId);

                if (result == null || !result.Any())
                    return Ok(new { message = "ℹ️ لا توجد مهام حالياً لهذا المندوب في منطقته.", tasks = Array.Empty<object>() });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} مهمة نشطة في منطقة المندوب.",
                    tasks = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب مهام المندوب {CourierId}", courierId);
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المهام", details = ex.Message });
            }
        }

        // ============================================================
        // 📦 2️⃣ تأكيد تسليم الوجبة من قبل المندوب
        // ============================================================
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] CreateDeliveryProofDto dto)
        {
            try
            {
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
        // 🧭 3️⃣ (للأدمن) جلب جميع عمليات التوصيل لجميع المندوبين
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _deliveryService.GetAllAsync();

                if (result == null || !result.Any())
                    return Ok(new { message = "ℹ️ لا توجد عمليات توصيل مسجلة حالياً.", deliveries = Array.Empty<object>() });

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
