namespace Sufra.Application.DTOs.Subscriptions
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }  // 🔹 اسم الطالب
        public string? UniversityId { get; set; } // 🔹 الرقم الجامعي
        public string PlanCode { get; set; } = ""; // 🔹 رمز الخطة (بدون تكرار)
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";
    }

    public class CreateSubscriptionDto
    {
        public int StudentId { get; set; }
        public string PlanCode { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateSubscriptionDto
    {
        public string? PlanCode { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
    }
}
