namespace Sufra.Domain.Entities
{
    public class HousingUnit
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // مثلاً "الوحدة 5"
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔹 علاقة عكسية
        public ICollection<StudentHousing> StudentHousings { get; set; } = new List<StudentHousing>();
    }
}
