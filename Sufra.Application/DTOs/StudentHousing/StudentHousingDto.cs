namespace Sufra.Application.DTOs.StudentHousing
{
    public class StudentHousingDto
    {
        public int StudentId { get; set; }
        public int ZoneId { get; set; }
        public int HousingUnitId { get; set; }
        public string RoomNo { get; set; } = string.Empty;
    }
}
