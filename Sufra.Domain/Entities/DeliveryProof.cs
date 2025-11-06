namespace Sufra.Domain.Entities
{
    public class DeliveryProof
    {
        public int Id { get; set; }

        // 🔹 العلاقة مع الطلب (1:1)
        public int MealRequestId { get; set; }
        public MealRequest MealRequest { get; set; } = default!;

        // 🔹 العلاقة مع المندوب
        public int CourierId { get; set; }
        public Courier Courier { get; set; } = default!;

        // 🔹 بيانات التوصيل
        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "pending"; // pending | delivering | delivered | failed
        public string? Notes { get; set; }
        public string? EvidenceUrl { get; set; }
        // public Zone Zone { get; set; }

        // 🔹 حالة تحقق المشرف
        public bool IsVerified { get; set; } = false;
        
    }
}
