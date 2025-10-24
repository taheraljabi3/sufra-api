using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Students;
using Sufra.Application.Interfaces;
using Sufra.Infrastructure.Persistence;
using BCrypt.Net; // 🔐 لتشفير والتحقق من كلمات المرور

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/students")]
    [Produces("application/json")]
    [Tags("👤 Students API")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly SufraDbContext _context;

        public StudentsController(IStudentService studentService, SufraDbContext context)
        {
            _studentService = studentService;
            _context = context;
        }

        // =====================================================================
        /// <summary>
        /// 📋 جلب جميع الطلاب مع الأدوار.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _context.Students
                .Select(s => new
                {
                    s.Id,
                    s.UniversityId,
                    s.Name,
                    s.Email,
                    s.Phone,
                    s.Status,
                    Role = s.Role ?? "Student",
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(result);
        }

        // =====================================================================
        /// <summary>
        /// 🔍 جلب طالب عبر الرقم الجامعي (University ID).
        /// </summary>
        [HttpGet("university/{universityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUniversityId(string universityId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UniversityId == universityId);

            if (student == null)
                return NotFound(new { message = "الطالب غير موجود بالرقم الجامعي" });

            return Ok(new
            {
                student.Id,
                student.UniversityId,
                student.Name,
                student.Email,
                student.Phone,
                student.Status,
                student.Role,
                student.CreatedAt
            });
        }

        // =====================================================================
        /// <summary>
        /// ➕ إنشاء طالب جديد (تسجيل حساب جديد).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔍 تحقق إذا الرقم الجامعي موجود مسبقًا
            bool exists = await _context.Students.AnyAsync(s => s.UniversityId == dto.UniversityId);
            if (exists)
                return Conflict(new { message = "الطالب موجود مسبقًا بنفس الرقم الجامعي" });

            // 🔒 تشفير كلمة المرور
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 🧱 إنشاء الكيان
            var student = new Domain.Entities.Student
            {
                UniversityId = dto.UniversityId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Password = hashedPassword,
                Role = "student",
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByUniversityId),
                new { universityId = student.UniversityId },
                new
                {
                    student.Id,
                    student.UniversityId,
                    student.Name,
                    student.Email,
                    student.Role,
                    message = "تم إنشاء الحساب بنجاح ✅"
                });
        }

        /// <summary>
/// 🔐 تسجيل الدخول باستخدام الرقم الجامعي وكلمة المرور.
/// </summary>
[HttpPost("login")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var student = await _context.Students
        .FirstOrDefaultAsync(s => s.UniversityId == dto.UniversityId);

    if (student == null)
        return NotFound(new { message = "الطالب غير موجود" });

    // ✅ تحقق من كلمة المرور (باستخدام BCrypt)
    bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, student.Password);
    if (!valid)
        return Unauthorized(new { message = "كلمة المرور غير صحيحة" });

    // ✅ ترجيع بيانات الطالب (بدون كلمة المرور)
    return Ok(new
    {
        student.Id,
        student.UniversityId,
        student.Name,
        student.Email,
        student.Role,
        student.Status
    });
}

        // =====================================================================
        /// <summary>
        /// ✏️ تحديث بيانات طالب عبر معرفه الداخلي (Id).
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _studentService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "الطالب غير موجود" });

            return Ok(result);
        }

        // =====================================================================
        /// <summary>
        /// 🗑️ حذف طالب عبر معرفه الداخلي (Id).
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _studentService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "الطالب غير موجود" });

            return NoContent();
        }
    }

    // =====================================================================
    /// <summary>
    /// DTO خاص بتسجيل الدخول.
    /// </summary>
    public class LoginDto
    {
        public string UniversityId { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
