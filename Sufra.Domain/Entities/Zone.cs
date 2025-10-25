namespace Sufra.Domain.Entities
{
    public class Zone
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Type { get; set; } = "housing"; // housing | classroom
        public string? ReferenceCode { get; set; }
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<StudentHousing> StudentHousings { get; set; } = new List<StudentHousing>();
        public ICollection<Courier> Couriers { get; set; } = new List<Courier>();
        public ICollection<MealRequest> MealRequests { get; set; } = new List<MealRequest>();
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();

        public DateTime? UpdatedAt { get; set; }

    }
}
