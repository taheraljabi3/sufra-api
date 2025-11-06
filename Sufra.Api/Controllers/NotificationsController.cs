using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICourierService _courierService; // 🟢 لإحضار بيانات المندوب ومنطقته
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ICourierService courierService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _courierService = courierService;
            _logger = logger;
        }

        // ============================================================
        // 📬 جلب جميع إشعارات المستخدم (حسب الدور)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetByUser([FromQuery] bool all = false)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var roleClaim = User.FindFirst("Role")?.Value ?? "student";

                if (userIdClaim == null)
                    return Unauthorized(new { message = "❌ لم يتم التعرف على المستخدم من التوكن." });

                int userId = int.Parse(userIdClaim);
                string normalizedRole = roleClaim.ToLower();

                var notifications = (normalizedRole == "owner" || normalizedRole == "admin" || all)
                    ? await _notificationService.GetByUserAsync(0, "owner")
                    : await _notificationService.GetByUserAsync(userId, normalizedRole);

                notifications = notifications.Where(n => n.IsActive).ToList();

                if (!notifications.Any())
                    return Ok(new
                    {
                        message = "ℹ️ لا توجد إشعارات حالياً.",
                        count = 0,
                        data = Array.Empty<NotificationDto>()
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
                _logger.LogError(ex, "❌ خطأ أثناء جلب الإشعارات للمستخدم");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإشعارات.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🆕 جلب الإشعارات غير المقروءة فقط
        // ============================================================
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread([FromQuery] bool all = false)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var roleClaim = User.FindFirst("Role")?.Value ?? "student";

                if (userIdClaim == null)
                    return Unauthorized(new { message = "❌ لم يتم التعرف على المستخدم من التوكن." });

                int userId = int.Parse(userIdClaim);
                string normalizedRole = roleClaim.ToLower();

                var notifications = (normalizedRole == "owner" || normalizedRole == "admin" || all)
                    ? await _notificationService.GetUnreadAsync(0, "owner")
                    : await _notificationService.GetUnreadAsync(userId, normalizedRole);

                notifications = notifications.Where(n => n.IsActive).ToList();

                if (!notifications.Any())
                    return Ok(new
                    {
                        message = "📭 لا توجد إشعارات غير مقروءة حالياً.",
                        count = 0,
                        data = Array.Empty<NotificationDto>()
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
                _logger.LogError(ex, "❌ خطأ أثناء جلب الإشعارات غير المقروءة");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإشعارات غير المقروءة.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🚴‍♂️ جلب إشعارات المندوبين في نفس المنطقة (وليس المندوب نفسه)
        // ============================================================
        [HttpGet("courier/{courierId:int}")]
        public async Task<IActionResult> GetForCourier(int courierId, [FromQuery] bool unreadOnly = false)
        {
            try
            {
                if (courierId <= 0)
                    return BadRequest(new { message = "⚠️ رقم المندوب غير صالح." });

                // 🏠 جلب بيانات المندوب لمعرفة ZoneId
                var courier = await _courierService.GetByIdAsync(courierId);
                if (courier == null)
                    return NotFound(new { message = $"🚫 لم يتم العثور على المندوب رقم {courierId}." });

                if (courier.ZoneId == null || courier.ZoneId <= 0)
                    return BadRequest(new { message = "⚠️ المندوب لا يملك منطقة محددة." });

int zoneId = courier.ZoneId;
                _logger.LogInformation("🚴‍♂️ جلب إشعارات لمنطقة ZoneId={ZoneId} الخاصة بالمندوب {CourierId}", zoneId, courierId);

                // 🔍 جلب الإشعارات الخاصة بالمنطقة فقط
                var notifications = await _notificationService.GetByZoneAsync(zoneId, unreadOnly);

                notifications = notifications.Where(n => n.IsActive).ToList();

                if (!notifications.Any())
                    return Ok(new
                    {
                        message = $"📭 لا توجد إشعارات حالية في المنطقة {zoneId}.",
                        count = 0,
                        data = Array.Empty<NotificationDto>()
                    });

                return Ok(new
                {
                    message = $"📫 تم جلب {notifications.Count()} إشعاراً في المنطقة {zoneId}.",
                    count = notifications.Count(),
                    data = notifications.OrderByDescending(n => n.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب إشعارات المنطقة الخاصة بالمندوب {CourierId}", courierId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب إشعارات المنطقة.",
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
        // 🚫 تعطيل الإشعارات المرتبطة بطلب معين
        // ============================================================
        [Authorize(Roles = "admin,owner,courier,student")]
        [HttpPut("deactivate/{requestId:int}")]
        public async Task<IActionResult> DeactivateByRequest(int requestId)
        {
            try
            {
                await _notificationService.DeactivateByRequestAsync(requestId);
                return Ok(new { message = $"🟡 تم تعطيل الإشعارات المرتبطة بالطلب رقم {requestId}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تعطيل الإشعارات المرتبطة بالطلب {RequestId}", requestId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تعطيل الإشعارات المرتبطة بالطلب.",
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
        // ➕ إنشاء إشعار يدوي
        // ============================================================
        [Authorize(Roles = "admin,owner,student")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "⚠️ بيانات الإشعار غير صالحة." });

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
        // ➕ إنشاء إشعارات جماعية
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateMany([FromBody] IEnumerable<NotificationDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                    return BadRequest(new { message = "⚠️ لا توجد إشعارات لإرسالها." });

                await _notificationService.CreateManyAsync(dtos);

                return Ok(new
                {
                    message = $"✅ تم إنشاء {dtos.Count()} إشعاراً جماعياً بنجاح.",
                    count = dtos.Count(),
                    data = dtos
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
