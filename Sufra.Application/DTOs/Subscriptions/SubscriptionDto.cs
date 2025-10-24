namespace Sufra.Application.DTOs.Subscriptions
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string PlanCode { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = default!;
    }
}
