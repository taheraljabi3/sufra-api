namespace Sufra.Domain.Entities
{
    public class BatchItem
    {
        public int BatchId { get; set; }
        public int ReqId { get; set; }

        // Navigation
        public Batch Batch { get; set; } = default!;
        public MealRequest MealRequest { get; set; } = default!;
    }
}
