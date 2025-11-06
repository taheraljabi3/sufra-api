namespace Sufra.Application.DTOs.Students
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string UniversityId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Password { get; set; } // ✅ جديد - لتحديث كلمة المرور
        public string? Role { get; set; } // ✅ جديد - لعرض الدور في الواجهة
        public string Status { get; set; } = default!;
        public int? CourierId { get; set; }
        public string? ZoneName { get; set; }
        public int? ZoneId { get; set; }
        public string? RoomNo { get; set; }

    }
}
