namespace Sufra.Domain.Entities
{
    public class MealRequest
    {
        public int Id { get; set; }

        // 🔹 علاقات رئيسية
        public int StudentId { get; set; }
        public int? SubscriptionId { get; set; }
        public int ZoneId { get; set; }

        // 🔹 بيانات الطلب
        public string Period { get; set; } = default!; // الإفطار | الغداء | العشاء
        public DateTime ReqTime { get; set; } = DateTime.UtcNow;
        public string DeliveryType { get; set; } = "room"; // room | pickup | توصيل
        public string LocationDetails { get; set; } = default!;
        public string? Notes { get; set; }

        // 🔹 الحالة
        // queued = في الانتظار
        // waiting_for_courier = بانتظار قبول المندوب
        // assigned = تم قبول الطلب
        // on_the_way = في الطريق
        // delivered = تم التسليم
        public string Status { get; set; } = "queued";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime MealDate { get; set; } = DateTime.UtcNow.Date;

        // 💰 حالة الدفع
        public bool IsPaid { get; set; } = false;

        // 🔹 علاقات الكيانات
        public Student Student { get; set; } = default!;
        public Subscription Subscription { get; set; } = default!;
        public Zone Zone { get; set; } = default!;
        public DeliveryProof? DeliveryProof { get; set; }

        public ICollection<BatchItem> BatchItems { get; set; } = new List<BatchItem>();

        // 🚴‍♂️ رقم المندوب المعيّن (قد يكون null قبل القبول)
        public int? AssignedCourierId { get; set; }

        // 🔗 مرجع المندوب المعين (اختياري لكن مفيد)
        public Courier? AssignedCourier { get; set; }
    }
}
