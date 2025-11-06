using System.Text.Json.Serialization;
using Sufra.Application.DTOs.Students;
using Sufra.Application.DTOs.Zones;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.DTOs.Couriers;

namespace Sufra.Application.DTOs.MealRequests
{
    /// <summary>
    /// 🎯 كائن نقل البيانات لطلبات الوجبات
    /// يُستخدم في عرض الطلبات في الـ API والواجهة الأمامية
    /// </summary>
    public class MealRequestDto
    {
        public int Id { get; set; }

        // 👤 بيانات الطالب
        [JsonPropertyName("StudentId")]
        public int StudentId { get; set; }

        [JsonPropertyName("Student")]
        public StudentDto? Student { get; set; }   // يتم تعبئتها من Include في الخدمة

        // 🏠 بيانات المنطقة
        [JsonPropertyName("ZoneId")]
        public int ZoneId { get; set; }

        [JsonPropertyName("Zone")]
        public ZoneDto? Zone { get; set; }

        // 💳 بيانات الاشتراك
        [JsonPropertyName("SubscriptionId")]
        public int? SubscriptionId { get; set; }

        [JsonPropertyName("Subscription")]
        public SubscriptionDto? Subscription { get; set; }

        // 🍽️ تفاصيل الطلب
        [JsonPropertyName("Period")]
        public string Period { get; set; } = string.Empty;  // الإفطار / الغداء / العشاء

        [JsonPropertyName("DeliveryType")]
        public string DeliveryType { get; set; } = string.Empty;  // توصيل / استلام ذاتي

        [JsonPropertyName("LocationDetails")]
        public string LocationDetails { get; set; } = string.Empty;

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; } = "queued";  // الحالة الافتراضية عند الإنشاء

        // ⏱️ أوقات الإنشاء والتحديث
        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; }

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
        public CourierDto? AssignedCourier { get; set; }

        // 💰 حالة الدفع
        [JsonPropertyName("IsPaid")]
        public bool IsPaid { get; set; }

        // 🏠 رقم الغرفة (جديد)
        [JsonPropertyName("RoomNo")]
        public string? RoomNo { get; set; }   // رقم غرفة الطالب من جدول StudentHousings
        // 📎 معلومات مختصرة إضافية لواجهة المستخدم
        [JsonPropertyName("StudentName")]
        public string? StudentName { get; set; }

        [JsonPropertyName("ZoneName")]
        public string? ZoneName { get; set; }
        public string? UniversityId { get; set; }

        [JsonPropertyName("CourierName")]
        public string? CourierName { get; set; }
    }
}
