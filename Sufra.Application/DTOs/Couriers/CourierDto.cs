namespace Sufra.Application.DTOs.Couriers
{
    public class CourierDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }

        
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public string VehicleType { get; set; } = default!;
        public int MaxCapacity { get; set; }
        public string Status { get; set; } = default!;
        public DateTime JoinedAt { get; set; }
    }
}
