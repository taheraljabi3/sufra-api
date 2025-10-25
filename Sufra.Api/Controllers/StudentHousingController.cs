using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.StudentHousing;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ تفعيل الحماية بالتوكن
    [ApiController]
    [Route("api/[controller]")]
    public class StudentHousingController : ControllerBase
    {
        private readonly IStudentHousingService _housingService;
        private readonly ILogger<StudentHousingController> _logger;

        public StudentHousingController(IStudentHousingService housingService, ILogger<StudentHousingController> logger)
        {
            _housingService = housingService;
            _logger = logger;
        }

        // =====================================================================
        /// <summary>
        /// 🏠 إضافة أو تحديث السكن الحالي للطالب (الوحدة + الغرفة + المنطقة)
        /// </summary>
        /// <remarks>
        /// يجب أن يُرسل الطلب بهذا الشكل:
        /// 
        /// {
        ///   "studentId": 1,
        ///   "housingUnitId": 12,
        ///   "roomNo": "455",
        ///   "zoneId": 1
        /// }
        /// </remarks>
        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertHousing([FromBody] StudentHousingDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "❌ لم يتم إرسال أي بيانات." });

                if (dto.StudentId <= 0)
                    return BadRequest(new { message = "❌ يجب تحديد معرف الطالب StudentId." });

                if (dto.HousingUnitId <= 0)
                    return BadRequest(new { message = "❌ يجب تحديد رقم الوحدة السكنية HousingUnitId." });

                if (string.IsNullOrWhiteSpace(dto.RoomNo))
                    return BadRequest(new { message = "❌ يجب إدخال رقم الغرفة RoomNo." });

                var success = await _housingService.UpsertHousingAsync(dto);

                if (success)
                    return Ok(new { message = "✅ تم حفظ الموقع بنجاح." });

                return StatusCode(500, new { message = "❌ حدث خطأ أثناء الحفظ." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تنفيذ UpsertHousing");
                return StatusCode(500, new { message = "⚠️ خطأ غير متوقع.", error = ex.Message });
            }
        }

        // =====================================================================
        /// <summary>
        /// 📍 جلب موقع الطالب الحالي (الوحدة والغرفة والمنطقة)
        /// </summary>
        /// <param name="studentId">معرف الطالب</param>
        /// <returns>بيانات السكن الحالي</returns>
        [AllowAnonymous] // ✅ مفتوح بدون توكن (اختياري)
        [HttpGet("{studentId:int}")]
        public async Task<IActionResult> GetCurrent(int studentId)
        {
            try
            {
                if (studentId <= 0)
                    return BadRequest(new { message = "❌ معرف الطالب غير صالح." });

                var result = await _housingService.GetCurrentAsync(studentId);

                if (result == null)
                    return NotFound(new { message = "⚠️ لم يتم العثور على موقع للطالب." });

                return Ok(new
                {
                    result.Id,
                    result.StudentId,
                    result.HousingUnitId,
                    result.RoomNo,
                    result.ZoneId,
                    ZoneName = result.Zone?.Name,
                    result.IsCurrent,
                    result.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تنفيذ GetCurrent");
                return StatusCode(500, new { message = "⚠️ خطأ غير متوقع.", error = ex.Message });
            }
        }
    }
}
