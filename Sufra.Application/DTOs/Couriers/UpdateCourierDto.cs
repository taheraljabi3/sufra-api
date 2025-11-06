namespace Sufra.Application.DTOs.Couriers
{
    public class UpdateCourierDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? VehicleType { get; set; }
        public int MaxCapacity { get; set; }
        public string? Status { get; set; }
        public int ZoneId { get; set; }
    }
}
