namespace Sufra.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;

        // 🔹 علاقة مع الطلاب
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
