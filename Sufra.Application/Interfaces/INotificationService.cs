using Sufra.Application.DTOs.Notifications;

namespace Sufra.Application.Interfaces
{
    public interface INotificationService
    {
        // ============================================================
        // ➕ إنشاء إشعار / إشعارات
        // ============================================================

        /// <summary>
        /// إنشاء إشعار واحد.
        /// </summary>
        Task CreateAsync(NotificationDto dto);

        /// <summary>
        /// إنشاء عدة إشعارات دفعة واحدة (Bulk).
        /// </summary>
        Task CreateManyAsync(IEnumerable<NotificationDto> notifications);

        // ============================================================
        // 📬 القراءة
        // ============================================================

        /// <summary>
        /// جلب جميع إشعارات المستخدم (مرتبة: غير المقروء أولاً ثم الأحدث).
        /// </summary>
        Task<IEnumerable<NotificationDto>> GetByUserAsync(int userId, string role);

        /// <summary>
        /// جلب الإشعارات غير المقروءة فقط للمستخدم.
        /// </summary>
        Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, string role);

        // ============================================================
        // ✅ التحديث / الحذف
        // ============================================================

        /// <summary>
        /// تحديد إشعار كمقروء.
        /// </summary>
        Task MarkAsReadAsync(int id);

        /// <summary>
        /// حذف إشعار.
        /// </summary>
        Task DeleteAsync(int id);

        Task<IEnumerable<NotificationDto>> GetByZoneAsync(int zoneId, bool unreadOnly = false);


        /// <summary>
        /// تعطيل (إلغاء تفعيل) جميع الإشعارات المرتبطة بطلب معيّن.
        /// تُستخدم عند قبول الطلب من أحد المندوبين ليختفي من الآخرين.
        /// </summary>
        Task DeactivateByRequestAsync(int requestId);
    }
}
