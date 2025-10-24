namespace Sufra.Application.DTOs.Deliveries
{
    public class DeliveryProofDto
    {
        public int Id { get; set; }
        public int MealRequestId { get; set; }
        public int CourierId { get; set; }
        public DateTime DeliveredAt { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "delivered";
        public string? EvidenceUrl { get; set; } // صورة أو توقيع
        
    }
}
