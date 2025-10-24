namespace Sufra.Domain.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public string UniversityId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }

        public string DefaultDeliveryType { get; set; } = "room"; // room | classroom
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Role { get; set; } = "student"; 
        public string? Password { get; set; } // ✅ العمود الجديد

     
        public ICollection<StudentHousing> Housings { get; set; } = new List<StudentHousing>();
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<MealRequest> MealRequests { get; set; } = new List<MealRequest>();
    }
}
