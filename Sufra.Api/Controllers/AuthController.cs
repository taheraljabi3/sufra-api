using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sufra.Application.DTOs.Auth;
using Sufra.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net; // 🔐 للتحقق من كلمات المرور

namespace Sufra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("🔐 Authentication API")]
    public class AuthController : ControllerBase
    {
        private readonly SufraDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(SufraDbContext context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // =====================================================================
        /// <summary>
        /// 🔐 تسجيل الدخول - التحقق من بيانات الطالب من قاعدة البيانات
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login([FromBody] StudentLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "❌ بيانات غير صالحة في النموذج" });

            // 🔍 البحث عن الطالب حسب الرقم الجامعي
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UniversityId == dto.UniversityId);

            if (student == null)
                return NotFound(new { message = "❌ الطالب غير موجود بالرقم الجامعي." });

            // 🔐 التحقق من كلمة المرور المشفرة
            bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, student.Password);
            if (!validPassword)
                return Unauthorized(new { message = "❌ كلمة المرور غير صحيحة." });

            // 🧠 توليد JWT Token
            var token = GenerateJwtToken(student);

            _logger.LogInformation("✅ تسجيل دخول ناجح للطالب {Name} ({UniversityId})", student.Name, student.UniversityId);

            return Ok(new
            {
                message = "✅ تسجيل الدخول ناجح",
                Id = student.Id,
                UniversityId = student.UniversityId,
                Name = student.Name,
                Email = student.Email,
                Role = student.Role,
                Status = student.Status,
                token
            });
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
                new Claim(ClaimTypes.Role, student.Role ?? "student"),
                new Claim("Status", student.Status ?? "active")
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
    public class StudentLoginDto
    {
        public string UniversityId { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
