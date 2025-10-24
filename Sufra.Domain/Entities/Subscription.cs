namespace Sufra.Domain.Entities
{
    public class Subscription
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string PlanCode { get; set; } = "monthly_30";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "active"; // active | expired | pending
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        

        // Navigation
        public Student Student { get; set; } = default!;
    }
}
