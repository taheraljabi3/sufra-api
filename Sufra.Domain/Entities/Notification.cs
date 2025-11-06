using System;

namespace Sufra.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        // 🔑 المستخدم المستهدف (الطالب / المندوب / المالك)
        public int UserId { get; set; }

        // 🧭 نوع المستخدم: student / courier / owner / admin
        public string Role { get; set; } = "student";

        // 📨 محتوى الإشعار
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // 🧭 المنطقة الجغرافية المرتبطة بالإشعار (قد تكون فارغة)
        public int? ZoneId { get; set; }

        // 🔗 ارتباط اختياري بطلب معين
        public int? RelatedRequestId { get; set; }

        // 🎓 المستخدم الطالب المرتبط بالإشعار (إن وجد)
        public int? StudentId { get; set; }

        // 🕓 وقت الإنشاء
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 📖 هل تمت قراءة الإشعار؟
        public bool IsRead { get; set; } = false;

        // 🟢 هل الإشعار نشط (ظاهر)؟ يُستخدم لتعطيله بعد قبول الطلب
        public bool IsActive { get; set; } = true;
    }
}
