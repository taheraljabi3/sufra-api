namespace Sufra.Application.DTOs.Zones
{
    public class ZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Type { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
