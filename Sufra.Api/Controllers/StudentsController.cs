using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sufra.Application.DTOs.Students;
using Sufra.Application.Interfaces;
using Sufra.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net; // 🔐 لتشفير والتحقق من كلمات المرور

namespace Sufra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("👤 Students API")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly SufraDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IStudentService studentService,
            SufraDbContext context,
            IConfiguration config,
            ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _context = context;
            _config = config;
            _logger = logger;
        }

        // =====================================================================
        /// <summary>
        /// 📋 جلب جميع الطلاب (للمشرف أو الأونر فقط)
        /// </summary>
        [Authorize(Roles = "admin,owner")]
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
                    Role = s.Role ?? "student",
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(result);
        }

        // =====================================================================
        /// <summary>
        /// 🔍 جلب طالب عبر الرقم الجامعي (للأدمن أو الأونر)
        /// </summary>
        [Authorize(Roles = "admin,owner")]
        [HttpGet("university/{universityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUniversityId(string universityId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UniversityId == universityId);

            if (student == null)
                return NotFound(new { message = "❌ الطالب غير موجود بالرقم الجامعي." });

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
        /// ➕ إنشاء حساب جديد للطالب (مفتوح بدون توكن)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] CreateStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool exists = await _context.Students.AnyAsync(s => s.UniversityId == dto.UniversityId);
            if (exists)
                return Conflict(new { message = "❌ الرقم الجامعي مستخدم مسبقًا." });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var student = new Domain.Entities.Student
            {
                UniversityId = dto.UniversityId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Password = hashedPassword,
                Role = "student", // 🔒 التسجيل العادي دائمًا طالب
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ تم إنشاء حساب جديد للطالب {Name}", student.Name);

            return CreatedAtAction(nameof(GetByUniversityId),
                new { universityId = student.UniversityId },
                new
                {
                    student.Id,
                    student.UniversityId,
                    student.Name,
                    student.Email,
                    student.Role,
                    message = "✅ تم إنشاء الحساب بنجاح"
                });
        }

        // =====================================================================
        /// <summary>
        /// 👑 إنشاء حساب جديد بواسطة الأونر (يتطلب صلاحية Owner)
        /// </summary>
        [Authorize(Roles = "owner")]
        [HttpPost("create-by-owner")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateByOwner([FromBody] CreateStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool exists = await _context.Students.AnyAsync(s => s.UniversityId == dto.UniversityId);
            if (exists)
                return Conflict(new { message = "❌ الرقم الجامعي مستخدم مسبقًا." });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var role = string.IsNullOrWhiteSpace(dto.Role) ? "student" : dto.Role.ToLower();

            var user = new Domain.Entities.Student
            {
                UniversityId = dto.UniversityId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Password = hashedPassword,
                Role = role,
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("👑 الأونر أنشأ مستخدمًا جديدًا {Name} بدور {Role}", user.Name, user.Role);

            return CreatedAtAction(nameof(GetByUniversityId),
                new { universityId = user.UniversityId },
                new
                {
                    user.Id,
                    user.UniversityId,
                    user.Name,
                    user.Email,
                    user.Role,
                    message = $"✅ تم إنشاء {user.Role} بنجاح بواسطة الأونر"
                });
        }

        // =====================================================================
        /// <summary>
        /// 🔐 تسجيل الدخول عبر الرقم الجامعي وكلمة المرور
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UniversityId == dto.UniversityId);

            if (student == null)
                return NotFound(new { message = "❌ الطالب غير موجود." });

            bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, student.Password);
            if (!valid)
                return Unauthorized(new { message = "❌ كلمة المرور غير صحيحة." });

            // ✅ توليد JWT Token
            var token = GenerateJwtToken(student);

            return Ok(new
            {
                message = "✅ تسجيل الدخول ناجح",
                student.Id,
                student.UniversityId,
                student.Name,
                student.Email,
                student.Role,
                student.Status,
                token
            });
        }

        // =====================================================================
        /// <summary>
        /// ✏️ تحديث بيانات الطالب (للأدمن أو الأونر)
        /// </summary>
        [Authorize(Roles = "admin,owner")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _studentService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "❌ الطالب غير موجود." });

            return Ok(result);
        }

        // =====================================================================
        /// <summary>
        /// 🗑️ حذف طالب (للأدمن أو الأونر)
        /// </summary>
        [Authorize(Roles = "admin,owner")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _studentService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "❌ الطالب غير موجود." });

            return NoContent();
        }

        // =====================================================================
        // 🧠 توليد JWT Token
        // =====================================================================
        private string GenerateJwtToken(Domain.Entities.Student student)
        {
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"] ?? "SUFRA_SECRET_KEY_2025_!CHANGE_THIS!");

            var claims = new[]
            {
                new Claim("UserId", student.Id.ToString()),
                new Claim("UniversityId", student.UniversityId),
                new Claim(ClaimTypes.Name, student.Name),
                new Claim(ClaimTypes.Role, student.Role ?? "student")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // =====================================================================
    // DTO لتسجيل الدخول
    // =====================================================================
    public class LoginDto
    {
        public string UniversityId { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
