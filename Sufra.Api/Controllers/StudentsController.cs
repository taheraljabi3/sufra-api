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
/// 📋 جلب جميع الطلاب مع بيانات السكن (للأدمن أو الأونر فقط)
/// </summary>
[Authorize(Roles = "admin,owner")]
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> GetAll()
{
    try
    {
        var result = await _studentService.GetAllAsync();

        if (!result.Any())
            return Ok(new { message = "⚠️ لا توجد سجلات طلاب حالياً." });

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ خطأ أثناء جلب بيانات الطلاب");
        return StatusCode(500, new { message = "⚠️ حدث خطأ أثناء تحميل بيانات الطلاب." });
    }
}

// =====================================================================
/// <summary>
/// 🔍 جلب طالب عبر الرقم الجامعي (مع بيانات السكن)
/// </summary>
[Authorize(Roles = "admin,owner")]
[HttpGet("university/{universityId}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetByUniversityId(string universityId)
{
    try
    {
        var student = await _studentService.GetByUniversityIdAsync(universityId);

        if (student == null)
            return NotFound(new { message = "❌ الطالب غير موجود بالرقم الجامعي." });

        return Ok(student);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ خطأ أثناء جلب بيانات الطالب");
        return StatusCode(500, new { message = "⚠️ حدث خطأ أثناء تحميل بيانات الطالب." });
    }
}
        // =====================================================================
        /// <summary>
        /// ➕ تسجيل طالب جديد بنفسه (دائمًا بدور student)
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
                Role = "student",
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
        /// 👑 إنشاء حساب جديد بواسطة الأونر (يمكن تحديد الدور)
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

            // 🚫 منع إنشاء Owner آخر
            if (role == "owner")
                return Forbid("🚫 لا يمكن إنشاء مستخدم بدور 'owner' جديد من النظام.");

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

    // ✅ نحاول جلب بيانات المندوب إذا كان هذا المستخدم من نوع Courier
    int? courierId = null;
    int? zoneId = null;

    if (student.Role != null && student.Role.ToLower() == "courier")
    {
        var courier = await _context.Couriers
            .FirstOrDefaultAsync(c => c.StudentId == student.Id);

        if (courier != null)
        {
            courierId = courier.Id;
            zoneId = courier.ZoneId; // ✅ نحفظ رقم المنطقة للمندوب
        }
    }

    var token = GenerateJwtToken(student);

    // ✅ إعادة جميع البيانات بما في ذلك ZoneId
    return Ok(new
    {
        message = "✅ تسجيل الدخول ناجح",
        Id = student.Id,
        UniversityId = student.UniversityId,
        Name = student.Name,
        Email = student.Email,
        Role = student.Role,
        Status = student.Status,
        CourierId = courierId, // 🔹 رقم المندوب
        ZoneId = zoneId,       // 🔹 رقم المنطقة الجديدة
        token = token
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

            var currentRole = User.FindFirstValue(ClaimTypes.Role);
            if (dto.Role != null && currentRole != "owner")
                return Forbid("🚫 فقط الـ Owner يمكنه تعديل الدور.");

            var result = await _studentService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "❌ الطالب غير موجود." });

            return Ok(result);
        }

      // =====================================================================
/// <summary>
/// 🗑️ حذف طالب (للأونر فقط)
/// </summary>
[Authorize(Roles = "owner")]
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id)
{
    try
    {
        var success = await _studentService.DeleteAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "❌ الطالب غير موجود."
            });
        }

        // ✅ في حال الحذف الناجح
        return Ok(new
        {
            message = "✅ تم حذف الطالب بنجاح."
        });
    }
    catch (DbUpdateException ex)
    {
        // ⚠️ في حال وجود علاقات مرتبطة (مثل MealRequests)
        Console.WriteLine($"⚠️ Delete failed for student {id}: {ex.InnerException?.Message ?? ex.Message}");
        return BadRequest(new
        {
            message = "❌ لا يمكن حذف الطالب لارتباطه بسجلات أخرى في النظام."
        });
    }
    catch (Exception ex)
    {
        // ⚠️ لأي خطأ غير متوقع
        Console.WriteLine($"❌ Unexpected delete error for student {id}: {ex.Message}");
        return StatusCode(500, new
        {
            message = "⚠️ حدث خطأ داخلي غير متوقع أثناء حذف الطالب."
        });
    }
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
