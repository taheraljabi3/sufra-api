using System.ComponentModel.DataAnnotations.Schema;

namespace Sufra.Domain.Entities
{
    [Table("StudentHousings")]
    public class StudentHousing
    {
        public int Id { get; set; }

        // 🔑 المفاتيح
        public int StudentId { get; set; }
        public int ZoneId { get; set; }
        public int HousingUnitId { get; set; }

        // 🏠 التفاصيل
        public string RoomNo { get; set; } = string.Empty;
        public bool IsCurrent { get; set; } = true;

        // 🕓 التواريخ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // 🔗 العلاقات
        [ForeignKey(nameof(StudentId))]
        public Student Student { get; set; } = default!;

        [ForeignKey(nameof(ZoneId))]
        public Zone? Zone { get; set; }
                public string? ZoneName
        {
            get
            {
                return Zone?.Name;
            }
        }


    }
}
