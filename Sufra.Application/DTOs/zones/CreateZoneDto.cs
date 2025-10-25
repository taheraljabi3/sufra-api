namespace Sufra.Application.DTOs.Zones
{
    public class CreateZoneDto
    {
        public string Name { get; set; } = default!;
        public string? ReferenceCode { get; set; }  // اختياري
        public string? Type { get; set; }           // مثال: "Housing" أو "Kitchen"
        public string Status { get; set; } = "active";
    }
}
