using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Sufra.Application.DTOs.Auth; // ✅ استدعاء DTO من المجلد الصحيح
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sufra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 🔐 تسجيل دخول الأدمن - يرجع JWT Token
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginDto dto)
        {
            // ⚙️ تحقق مبدئي - بدله لاحقًا باستعلام قاعدة بيانات حقيقي
            if (dto.Username == "admin" && dto.Password == "1234")
            {
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"] ?? "SUFRA_SECRET_KEY_2025_!CHANGE_THIS!");
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, dto.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(12),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return Ok(new
                {
                    message = "✅ تسجيل الدخول ناجح",
                    token = tokenHandler.WriteToken(token)
                });
            }

            return Unauthorized("❌ اسم المستخدم أو كلمة المرور غير صحيحة");
        }
    }
}
