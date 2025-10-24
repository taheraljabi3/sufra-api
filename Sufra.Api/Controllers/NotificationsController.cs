using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // 📬 جلب جميع إشعارات المستخدم (مع دعم role=owner)
        // ============================================================
        [HttpGet("{role}/{userId:int}")]
        public async Task<IActionResult> GetByUser(string role, int userId, [FromQuery] bool all = false)
        {
            try
            {
                var normalizedRole = role.ToLower();

                // 👑 المالك يشاهد جميع الإشعارات (بدون فلترة userId/role)
                var notifications = normalizedRole == "owner" || all
                    ? await _notificationService.GetByUserAsync(0, "owner")
                    : await _notificationService.GetByUserAsync(userId, normalizedRole);

                if (!notifications.Any())
                    return Ok(new
                    {
                        message = "ℹ️ لا توجد إشعارات حالياً.",
                        count = 0,
                        data = Array.Empty<object>()
                    });

                return Ok(new
                {
                    message = $"✅ تم جلب {notifications.Count()} إشعاراً بنجاح.",
                    count = notifications.Count(),
                    data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب الإشعارات للمستخدم {UserId}", userId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإشعارات.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🆕 جلب الإشعارات غير المقروءة فقط (مع دعم owner)
        // ============================================================
        [HttpGet("{role}/{userId:int}/unread")]
        public async Task<IActionResult> GetUnread(string role, int userId, [FromQuery] bool all = false)
        {
            try
            {
                var normalizedRole = role.ToLower();

                // 👑 المالك يشاهد جميع الإشعارات غير المقروءة في النظام
                var notifications = normalizedRole == "owner" || all
                    ? await _notificationService.GetUnreadAsync(0, "owner")
                    : await _notificationService.GetUnreadAsync(userId, normalizedRole);

                if (!notifications.Any())
                    return Ok(new
                    {
                        message = "📭 لا توجد إشعارات غير مقروءة حالياً.",
                        count = 0,
                        data = Array.Empty<object>()
                    });

                return Ok(new
                {
                    message = $"📫 تم جلب {notifications.Count()} إشعاراً غير مقروء.",
                    count = notifications.Count(),
                    data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب الإشعارات غير المقروءة للمستخدم {UserId}", userId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإشعارات غير المقروءة.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // ✅ تحديد إشعار كمقروء
        // ============================================================
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Ok(new { message = $"✅ تم تحديد الإشعار ({id}) كمقروء." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تحديد الإشعار كمقروء {Id}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تحديد الإشعار كمقروء.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🗑️ حذف إشعار
        // ============================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _notificationService.DeleteAsync(id);
                return Ok(new { message = $"🗑️ تم حذف الإشعار رقم {id}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في حذف الإشعار {Id}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء حذف الإشعار.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // ➕ إنشاء إشعار يدوي (اختياري - للاختبار أو المشرفين)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        {
            try
            {
                await _notificationService.CreateAsync(dto);
                return Ok(new
                {
                    message = "✅ تم إنشاء الإشعار بنجاح.",
                    data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في إنشاء إشعار جديد");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء إنشاء الإشعار.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // ➕ إنشاء إشعارات جماعية (مثلاً عند إشعار عدة مندوبين دفعة واحدة)
        // ============================================================
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateMany([FromBody] IEnumerable<NotificationDto> dtos)
        {
            try
            {
                await _notificationService.CreateManyAsync(dtos);
                return Ok(new
                {
                    message = $"✅ تم إنشاء {dtos.Count()} إشعاراً جماعياً بنجاح.",
                    count = dtos.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في إنشاء إشعارات جماعية");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء إنشاء إشعارات جماعية.",
                    details = ex.Message
                });
            }
        }
    }
}
