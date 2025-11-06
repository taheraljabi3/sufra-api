namespace Sufra.Application.DTOs.Deliveries
{
    public class CreateDeliveryProofDto
    {
        public int MealRequestId { get; set; }
        public int CourierId { get; set; }
        public string? CourierName { get; set; }      // ✅ اسم المندوب
        public string? StudentName { get; set; }      // ✅ اسم الطالب
        public string? ZoneName { get; set; }   
        public string? Notes { get; set; }
        public string? EvidenceUrl { get; set; }
    }
}
