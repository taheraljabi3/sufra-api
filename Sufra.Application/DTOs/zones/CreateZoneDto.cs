// 📄 CreateZoneDto.cs
namespace Sufra.Application.DTOs.Zones
{
    public class CreateZoneDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
    }
}
