namespace Sufra.Domain.Entities
{
    public class Batch
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public string Period { get; set; } = default!; // breakfast | lunch | dinner
        public DateTime PickupTime { get; set; }
        public int CourierId { get; set; }
        public string Status { get; set; } = "pending"; // pending | in_progress | completed | cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Zone Zone { get; set; } = default!;
        public Courier Courier { get; set; } = default!;
        public ICollection<BatchItem> Items { get; set; } = new List<BatchItem>();
    }
}
