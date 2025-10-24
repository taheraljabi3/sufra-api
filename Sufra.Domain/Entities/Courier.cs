namespace Sufra.Domain.Entities
{
    public class Courier
    {
        public int Id { get; set; }
        public int StudentId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public string VehicleType { get; set; } = "scooter"; // scooter | bike | walk
        public int MaxCapacity { get; set; } = 10;
        public string Status { get; set; } = "active"; // active | off | suspended
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Student Student { get; set; } = default!;
        public Zone Zone { get; set; } = default!;
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();

        // 🚚 كل المرات التي قام فيها هذا المندوب بالتوصيل
        public ICollection<DeliveryProof> DeliveryProofs { get; set; } = new List<DeliveryProof>();

    }
}
