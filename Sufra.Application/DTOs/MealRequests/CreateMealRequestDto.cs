namespace Sufra.Application.DTOs.MealRequests
{
    public class CreateMealRequestDto
    {
        public int StudentId { get; set; }

        public int? SubscriptionId { get; set; }

        public string Period { get; set; } = default!; // breakfast | lunch | dinner
        public string DeliveryType { get; set; } = "room"; // room | classroom
        public int ZoneId { get; set; }
        public string LocationDetails { get; set; } = default!;
        public string? Notes { get; set; }
    }
}
