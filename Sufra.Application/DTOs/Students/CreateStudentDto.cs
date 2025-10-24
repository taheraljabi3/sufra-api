namespace Sufra.Application.DTOs.Students
{
    /// <summary>
    /// 📦 كائن نقل البيانات المستخدم عند إنشاء حساب طالب جديد.
    /// </summary>
    public class CreateStudentDto
    {
        /// <summary>
        /// الرقم الجامعي للطالب (Unique Identifier).
        /// </summary>
        public string UniversityId { get; set; } = default!;

        /// <summary>
        /// الاسم الكامل للطالب.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// البريد الإلكتروني الجامعي.
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// رقم الجوال (اختياري).
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// كلمة المرور الخاصة بالطالب (يتم تشفيرها قبل الحفظ).
        /// </summary>
        public string Password { get; set; } = default!;

        /// <summary>
        /// الدور الافتراضي للطالب (عادة Student).
        /// </summary>
        public string? RoleName { get; set; } = "Student";
        
    }
}
