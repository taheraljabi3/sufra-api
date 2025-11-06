using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ حماية جميع العمليات بالـ JWT
    [ApiController]
    [Route("api/[controller]")]
    [Tags("📦 Subscriptions API")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionsController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        // ============================================================
        /// <summary>📋 جلب جميع الاشتراكات (للمشرف فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _subscriptionService.GetAllAsync();
            return Ok(new
            {
                message = $"✅ تم جلب {result.Count()} اشتراكًا.",
                data = result
            });
        }

        // ============================================================
        /// <summary>🔍 جلب اشتراك محدد بالمعرف</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner,student")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _subscriptionService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "❌ الاشتراك غير موجود." });

            return Ok(result);
        }

        // ============================================================
        /// <summary>📦 جلب الاشتراك النشط للطالب الحالي</summary>
        // ============================================================
        [Authorize(Roles = "student,admin,owner")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveForCurrentStudent()
        {
            // 🧭 استخراج المعرف من التوكن
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var roleClaim = User.FindFirst("Role")?.Value ?? "student";

            if (userIdClaim == null)
                return Unauthorized(new { message = "❌ لم يتم التعرف على المستخدم من التوكن." });

            int studentId = int.Parse(userIdClaim);

            // 👑 الأدمن يمكنه تمرير query إذا أراد جلب اشتراك طالب آخر
            if (roleClaim is "Admin" or "Owner" && Request.Query.ContainsKey("studentId"))
            {
                int.TryParse(Request.Query["studentId"], out studentId);
            }

            var result = await _subscriptionService.GetActiveByStudentAsync(studentId);
            if (result == null)
                return NotFound(new { message = "⚠️ لا يوجد اشتراك نشط لهذا الطالب." });

            return Ok(new
            {
                message = "✅ تم جلب الاشتراك النشط بنجاح.",
                data = result
            });
        }

        // ============================================================
        /// <summary>➕ إنشاء اشتراك جديد (للأدمن فقط)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
        {
            try
            {
                var result = await _subscriptionService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, new
                {
                    message = "✅ تم إنشاء الاشتراك بنجاح.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "⚠️ تعارض أثناء إنشاء الاشتراك");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل أثناء إنشاء الاشتراك");
                return StatusCode(500, new { message = "حدث خطأ داخلي.", details = ex.Message });
            }
        }
// ✏️ تحديث اشتراك (للأدمن فقط)
[Authorize(Roles = "admin,owner")]
[HttpPut("{id:int}")]
public async Task<IActionResult> Update(int id, [FromBody] UpdateSubscriptionDto dto)
{
    try
    {
        var result = await _subscriptionService.UpdateAsync(id, dto);
        if (result == null)
            return NotFound(new { message = "❌ الاشتراك غير موجود." });

        return Ok(new
        {
            message = "✅ تم تحديث الاشتراك بنجاح.",
            data = result
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ خطأ أثناء تحديث الاشتراك {Id}", id);
        return StatusCode(500, new { message = "حدث خطأ أثناء التحديث.", details = ex.Message });
    }
}

        // ============================================================
        /// <summary>❌ إلغاء اشتراك (للأدمن أو الطالب نفسه)</summary>
        // ============================================================
        [Authorize(Roles = "admin,owner,student")]
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var success = await _subscriptionService.CancelAsync(id);
                if (!success)
                    return NotFound(new { message = "❌ الاشتراك غير موجود." });

                _logger.LogInformation("✅ تم إلغاء الاشتراك {Id} بنجاح", id);
                return Ok(new { message = "✅ تم إلغاء الاشتراك بنجاح." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء إلغاء الاشتراك {Id}", id);
                return StatusCode(500, new { message = "حدث خطأ أثناء إلغاء الاشتراك.", details = ex.Message });
            }
        }
    }
}
