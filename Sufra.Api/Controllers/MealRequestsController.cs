using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;

namespace Sufra.API.Controllers
{
    [Authorize] // ✅ حماية جميع العمليات بتوكن JWT
    [ApiController]
    [Route("api/[controller]")]
    public class MealRequestsController : ControllerBase
    {
        private readonly IMealRequestService _mealRequestService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MealRequestsController> _logger;

        public MealRequestsController(
            IMealRequestService mealRequestService,
            INotificationService notificationService,
            ILogger<MealRequestsController> logger)
        {
            _mealRequestService = mealRequestService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // 🧾 جلب جميع الطلبات (للمشرف فقط)
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mealRequestService.GetAllAsync();
            return Ok(new
            {
                message = $"✅ تم جلب {result.Count()} طلبًا.",
                data = result
            });
        }

        // ============================================================
        // 🧍‍♂️ جلب الطلبات الخاصة بالطالب (مفتوح للطالب بنفسه)
        // ============================================================
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var result = await _mealRequestService.GetByStudentAsync(studentId);
            if (!result.Any())
                return Ok(new { message = "ℹ️ لا توجد طلبات حالياً لهذا الطالب.", data = Array.Empty<object>() });

            return Ok(new
            {
                message = $"✅ تم جلب {result.Count()} طلباً للطالب رقم {studentId}.",
                data = result
            });
        }

        // ============================================================
        // 🚴‍♂️ جلب الطلبات حسب المندوب (مفتوح للمندوبين فقط)
        // ============================================================
        [Authorize(Roles = "Courier,Admin")]
        [HttpGet("courier/{courierId}")]
        public async Task<IActionResult> GetByCourier(int courierId)
        {
            try
            {
                var result = await _mealRequestService.GetByCourierAsync(courierId);

                if (!result.Any())
                    return Ok(new
                    {
                        message = "ℹ️ لا توجد طلبات حالياً في منطقة هذا المندوب.",
                        tasks = Array.Empty<object>()
                    });

                return Ok(new
                {
                    message = $"✅ تم جلب {result.Count()} مهمة نشطة في منطقة المندوب.",
                    tasks = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء جلب طلبات المندوب {CourierId}", courierId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب طلبات المندوب.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🔍 جلب طلب واحد (مفتوح للجميع بشرط أن يكون صاحب الطلب)
        // ============================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mealRequestService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = $"❌ الطلب بالمعرّف ({id}) غير موجود." });

            return Ok(result);
        }

        // ============================================================
        // 📢 تحديث حالة الطلب الحالي وإرسال إشعارات للمندوبين في نفس المنطقة
        // ============================================================
        [Authorize(Roles = "Student,Admin")] // الطالب هو من يرسل الطلب غالبًا
        [HttpPost("notify")]
        public async Task<IActionResult> NotifyCouriers([FromBody] CreateMealRequestDto dto)
        {
            try
            {
                var result = await _mealRequestService.NotifyCouriersOnlyAsync(dto);

                if (result == null)
                    return NotFound(new
                    {
                        message = "⚠️ لم يتم العثور على طلب مطابق لهذا الطالب في هذا اليوم أو الفترة."
                    });

                return Ok(new
                {
                    message = "✅ تم تحديث حالة الطلب وإشعار المندوبين بنجاح.",
                    requestId = result.Id,
                    newStatus = result.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء إشعار المندوبين للطالب {StudentId}", dto.StudentId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تحديث الطلب أو إرسال الإشعارات.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🚴‍♂️ قبول الطلب من أحد المندوبين
        // ============================================================
        [Authorize(Roles = "Courier,Admin")]
        [HttpPut("{requestId:int}/accept/{courierId:int}")]
        public async Task<IActionResult> AcceptRequest(int requestId, int courierId)
        {
            try
            {
                var result = await _mealRequestService.AssignCourierAsync(requestId, courierId);

                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                _logger.LogInformation("✅ الطلب {RequestId} تم قبوله من المندوب {CourierId}.", requestId, courierId);

                return Ok(new
                {
                    message = result.Message,
                    requestId,
                    courierId,
                    studentId = result.StudentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء قبول الطلب {RequestId}", requestId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء قبول الطلب.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🔄 تحديث حالة الطلب (من المندوب أو الأدمن)
        // ============================================================
        [Authorize(Roles = "Courier,Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateMealRequestStatusDto dto)
        {
            try
            {
                var request = await _mealRequestService.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = $"❌ الطلب بالمعرّف {id} غير موجود." });

                if (request.Status == dto.Status)
                    return BadRequest(new { message = "⚠️ الحالة الجديدة مطابقة للحالية." });

                request.Status = dto.Status;
                request.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? request.Notes : dto.Notes;

                await _mealRequestService.UpdateAsync(request);

                _logger.LogInformation("✅ تم تحديث حالة الطلب {Id} إلى {Status}", id, dto.Status);

                return Ok(new
                {
                    message = $"✅ تم تحديث حالة الطلب إلى {dto.Status}.",
                    id,
                    newStatus = dto.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تحديث حالة الطلب {Id}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تحديث حالة الطلب.",
                    details = ex.Message
                });
            }
        }
    }

    // ============================================================
    // 🧩 DTO لتحديث الحالة
    // ============================================================
    public class UpdateMealRequestStatusDto
    {
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
    }
}
