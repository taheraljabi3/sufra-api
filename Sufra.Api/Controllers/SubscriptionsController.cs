using System.Security.Claims;
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

        // ثوابت الأدوار لتفادي أخطاء حالة الأحرف
        private const string RoleAdmin = "admin";
        private const string RoleOwner = "owner";
        private const string RoleStudent = "student";

        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionsController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        // ===================== Helpers ======================
        private int? TryGetUserId()
        {
            // أولًا نحاول مع Claim مخصص باسم UserId إن وجد
            var userId = User.FindFirst("UserId")?.Value;

            // أو المعرف القياسي للمستخدم (NameIdentifier / sub)
            userId ??= User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            userId ??= User.FindFirst("sub")?.Value;

            if (int.TryParse(userId, out var id))
                return id;

            return null;
        }

        private bool IsInRole(params string[] roles)
        {
            foreach (var r in roles)
            {
                // IsInRole يعتمد على المطابقة النصية؛ نوحّد للأحرف الصغيرة
                if (User.IsInRole(r) || User.Claims.Any(c =>
                        c.Type == ClaimTypes.Role && string.Equals(c.Value, r, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }
        // ====================================================

        /// <summary>📋 جلب جميع الاشتراكات (للمشرف فقط)</summary>
        [Authorize(Roles = $"{RoleAdmin},{RoleOwner}")]
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

        /// <summary>🔍 جلب اشتراك محدد بالمعرف</summary>
        [Authorize(Roles = $"{RoleAdmin},{RoleOwner},{RoleStudent}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _subscriptionService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "❌ الاشتراك غير موجود." });

            return Ok(result);
        }

        /// <summary>📦 جلب الاشتراك النشط للطالب الحالي</summary>
        /// <param name="studentId">إن تم تمريره من الأدمن/المالك سيُستخدم بدل المعرّف القادم من التوكن</param>
        [Authorize(Roles = $"{RoleStudent},{RoleAdmin},{RoleOwner}")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveForCurrentStudent([FromQuery] int? studentId = null)
        {
            // 🧭 استخراج المعرف من التوكن
            var currentUserId = TryGetUserId();
            if (currentUserId is null)
                return Unauthorized(new { message = "❌ لم يتم التعرف على المستخدم من التوكن." });

            // 👑 الأدمن أو المالك يمكنه الاستعلام عن طالب آخر
            int effectiveStudentId = currentUserId.Value;
            if (studentId.HasValue && IsInRole(RoleAdmin, RoleOwner))
                effectiveStudentId = studentId.Value;

            var result = await _subscriptionService.GetActiveByStudentAsync(effectiveStudentId);
            if (result == null)
                return NotFound(new { message = "⚠️ لا يوجد اشتراك نشط لهذا الطالب." });

            return Ok(new
            {
                message = "✅ تم جلب الاشتراك النشط بنجاح.",
                data = result
            });
        }

        /// <summary>➕ إنشاء اشتراك جديد (للأدمن فقط)</summary>
        [Authorize(Roles = $"{RoleAdmin},{RoleOwner}")]
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

        /// <summary>✏️ تحديث اشتراك (للأدمن فقط)</summary>
        [Authorize(Roles = $"{RoleAdmin},{RoleOwner}")]
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

        /// <summary>❌ إلغاء اشتراك (للأدمن أو الطالب نفسه)</summary>
        [Authorize(Roles = $"{RoleAdmin},{RoleOwner},{RoleStudent}")]
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
