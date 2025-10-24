using System;

namespace Sufra.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        // 🔑 المستخدم المستهدف
        public int UserId { get; set; }

        // 🧭 نوع المستخدم: student / courier / admin
        public string Role { get; set; } = "student";

        // 📨 محتوى الإشعار
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // 🔗 ارتباط اختياري بطلب معين
        public int? RelatedRequestId { get; set; }

        // 🕓 أوقات الإنشاء والقراءة
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
