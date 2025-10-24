namespace Sufra.Application.DTOs.Batches
{
    public class BatchDto
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public string Period { get; set; } = default!;
        public int CourierId { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public int TotalRequests { get; set; }
    }
}
