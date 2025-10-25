// 📄 UpdateZoneDto.cs
namespace Sufra.Application.DTOs.Zones
{
    public class UpdateZoneDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Status { get; set; }
    }
}
