using System.Text.Json.Serialization;
using Sufra.Application.DTOs.Students;
using Sufra.Application.DTOs.Zones;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.DTOs.Couriers; // ✅ لتضمين بيانات المندوب (إن أردت إظهارها في الـ API)

namespace Sufra.Application.DTOs.MealRequests
{
    public class MealRequestDto
    {
        public int Id { get; set; }

        // 👤 بيانات الطالب
        [JsonPropertyName("StudentId")]
        public int StudentId { get; set; }

        [JsonPropertyName("Student")]
        public StudentDto? Student { get; set; } = new StudentDto
        {
            Name = "غير معروف",
            UniversityId = "—",
            Status = "inactive"
        };

        // 🏠 بيانات المنطقة
        [JsonPropertyName("ZoneId")]
        public int ZoneId { get; set; }

        [JsonPropertyName("Zone")]
        public ZoneDto? Zone { get; set; } = new ZoneDto
        {
            Name = "غير معروف"
        };

        // 💳 بيانات الاشتراك
        [JsonPropertyName("SubscriptionId")]
        public int? SubscriptionId { get; set; }

        [JsonPropertyName("Subscription")]
        public SubscriptionDto? Subscription { get; set; } = new SubscriptionDto
        {
            Status = "غير محدد"
        };

        // 🍽️ تفاصيل الطلب
        [JsonPropertyName("Period")]
        public string Period { get; set; } = string.Empty;         // الإفطار / الغداء / العشاء

        [JsonPropertyName("DeliveryType")]
        public string DeliveryType { get; set; } = string.Empty;   // توصيل / استلام ذاتي

        [JsonPropertyName("LocationDetails")]
        public string LocationDetails { get; set; } = string.Empty;

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; } = string.Empty;

        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;         // queued / waiting_for_courier / assigned / on_the_way / delivered

        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // 📅 تاريخ الوجبة الفعلي
        [JsonPropertyName("MealDate")]
        public DateTime? MealDate { get; set; }

        [JsonPropertyName("ReqTime")]
        public DateTime? ReqTime { get; set; }

        // 🚴‍♂️ المندوب المعيّن (إن وُجد)
        [JsonPropertyName("AssignedCourierId")]
        public int? AssignedCourierId { get; set; }

        [JsonPropertyName("AssignedCourier")]
        public CourierDto? AssignedCourier { get; set; } // ✅ إضافة اختيارية لعرض بيانات المندوب (الاسم / الجوال / إلخ)

        // 💰 حالة الدفع
        [JsonPropertyName("IsPaid")]
        public bool IsPaid { get; set; } = false;

        // 📎 معلومات إضافية مختصرة للواجهة
        public string? StudentName { get; set; }
        public string? ZoneName { get; set; }
        public string? CourierName { get; set; } // ✅ لتسهيل العرض السريع بدون Include كامل
    }
}
