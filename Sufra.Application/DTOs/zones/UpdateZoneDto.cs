namespace Sufra.Application.DTOs.Zones
{
    public class UpdateZoneDto
    {
        public string? Name { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Type { get; set; } // مثل "سكن" أو "مطبخ"
        public string? Status { get; set; } // active / inactive
    }
}
