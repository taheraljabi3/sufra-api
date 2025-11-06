namespace Sufra.Application.DTOs.Deliveries
{
    public class DeliveryProofDto
    {
    public int Id { get; set; }
    public int MealRequestId { get; set; }
    public int CourierId { get; set; }

    public string? CourierName { get; set; }
    public string? StudentName { get; set; }
    public string? ZoneName { get; set; }
    public string? RoomNo { get; set; }

    public string Status { get; set; } = "قيد التوصيل";
    public string? Notes { get; set; }
    public string? EvidenceUrl { get; set; }
    public DateTime? DeliveredAt { get; set; }
    }
}
