using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Batches;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize] // 🔐 تفعيل المصادقة بالتوكن على مستوى الكنترولر
    [ApiController]
    [Route("api/[controller]")]
    [Tags("📦 Batches API")]
    public class BatchesController : ControllerBase
    {
        private readonly IBatchService _batchService;
        private readonly ILogger<BatchesController> _logger;

        public BatchesController(IBatchService batchService, ILogger<BatchesController> logger)
        {
            _batchService = batchService;
            _logger = logger;
        }

        // ============================================================
        /// <summary>📋 جلب جميع الدُفعات (Batches) — للمالك أو الأدمن فقط</summary>
        // ============================================================
        [Authorize(Roles = "Admin,Owner")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _batchService.GetAllAsync();

                if (result == null || !result.Any())
                    return Ok(new { message = "ℹ️ لا توجد دفعات حالياً.", batches = Array.Empty<object>() });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} دفعة بنجاح.",
                    batches = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب جميع الدُفعات");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الدُفعات", details = ex.Message });
            }
        }

        // ============================================================
        /// <summary>➕ إنشاء دفعة جديدة (Batch) — للمالك فقط</summary>
        // ============================================================
        [Authorize(Roles = "Owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBatchDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "❌ لم يتم إرسال بيانات الدفعة." });

                var result = await _batchService.CreateAsync(dto);

                return CreatedAtAction(nameof(GetAll), new { id = result.Id }, new
                {
                    message = "✅ تم إنشاء الدفعة الجديدة بنجاح.",
                    batch = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "⚠️ خطأ منطقي أثناء إنشاء الدفعة");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في إنشاء الدفعة الجديدة");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الدفعة", details = ex.Message });
            }
        }
    }
}
