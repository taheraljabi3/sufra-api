namespace Sufra.Application.DTOs.Couriers
{
    public class CreateCourierDto
    {
        public int StudentId { get; set; }
        public int ZoneId { get; set; }
        public string VehicleType { get; set; } = "scooter"; // scooter | bike | walk
        public int MaxCapacity { get; set; } = 10;
    }
}
