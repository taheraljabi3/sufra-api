namespace Sufra.Application.DTOs.MealRequests
{
    public class CreateMealRequestFullDto
    {
        public int StudentId { get; set; }
        public int SubscriptionId { get; set; }
        public int ZoneId { get; set; }
        public string Period { get; set; } = default!;
        public string DeliveryType { get; set; } = default!;
        public string LocationDetails { get; set; } = default!;
        public string? Notes { get; set; }
        public string Status { get; set; } = "queued";
        public bool IsPaid { get; set; } = false;
        public DateTime MealDate { get; set; } = DateTime.Today;
        public int? AssignedCourierId { get; set; }
    }
}
