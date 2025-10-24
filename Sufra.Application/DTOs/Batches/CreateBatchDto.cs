namespace Sufra.Application.DTOs.Batches
{
    public class CreateBatchDto
    {
        public int ZoneId { get; set; }
        public string Period { get; set; } = default!; // breakfast | lunch | dinner
        public int CourierId { get; set; }
    }
}
