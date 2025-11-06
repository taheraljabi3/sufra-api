namespace Sufra.Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Role { get; set; } = "student";

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // 🔗 يربط الإشعار بطلب معين (اختياري)
        public int? RelatedRequestId { get; set; }

        // 🧭 المنطقة الجغرافية (قد تكون فارغة)
        public int? ZoneId { get; set; }

        // 🧑‍🎓 ربط الإشعار بصاحب الطلب (الطالب)
        public int? StudentId { get; set; }

        // 📅 وقت الإنشاء
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 📖 هل تمت قراءته؟
        public bool IsRead { get; set; } = false;

        // 🟢 هل الإشعار نشط؟ (يُعرض للمستخدم)
        public bool IsActive { get; set; } = true;

        public bool CanAccept { get; set; }

    }
}
