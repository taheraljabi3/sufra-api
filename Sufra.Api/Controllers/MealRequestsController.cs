using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;


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
        // 📦 إنشاء دفعة وجبات (للأدمن فقط)
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] List<CreateMealRequestFullDto> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                    return BadRequest(new { message = "⚠️ لا توجد وجبات للإدخال." });

                var result = await _mealRequestService.BulkCreateAsync(requests);

                _logger.LogInformation("✅ تم إدخال {Count} وجبة دفعة واحدة بنجاح من قبل {User}.",
                    result.Count(), User.Identity?.Name ?? "مستخدم غير معروف");

                return Ok(new
                {
                    message = $"✅ تم إدخال {result.Count()} وجبة بنجاح.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "⚠️ خطأ منطقي أثناء الإدخال الدفعي للوجبات.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء الإدخال الدفعي للوجبات.");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء الإدخال الدفعي للوجبات.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🏗️ إنشاء وجبة جديدة (للأدمن فقط)
        // ============================================================
        [Authorize(Roles = "admin,owner")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateMealRequest([FromBody] CreateMealRequestFullDto dto)
        {
            try
            {
                var result = await _mealRequestService.CreateAdminAsync(dto);

                return Ok(new
                {
                    message = "✅ تم إنشاء الوجبة بنجاح.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء إنشاء الوجبة للطالب {StudentId}", dto.StudentId);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء إنشاء الوجبة.",
                    details = ex.Message
                });
            }
        }

        // ============================================================
        // 🧾 جلب جميع الطلبات (للمشرف فقط)
        // ============================================================
        [Authorize(Roles = "admin,owner")]
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
        [Authorize(Roles = "courier,admin")]
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
   
        [Authorize(Roles = "student,admin,owner")]
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
                    zoneId = result.ZoneId,
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
        // 🚴‍♂️ قبول الطلب من أحد المندوبين (مع تعطيل إشعارات البقية)
        // ============================================================
        [Authorize(Roles = "courier,admin,owner")]
        [HttpPut("{requestId:int}/accept/{courierId:int}")]
        public async Task<IActionResult> AcceptRequest(int requestId, int courierId)
        {
            try
            {
                // ✅ تنفيذ المنطق الأساسي في الخدمة
                var result = await _mealRequestService.AssignCourierAsync(requestId, courierId);
                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                // 🚫 تعطيل الإشعارات الأخرى لنفس الطلب
                await _notificationService.DeactivateByRequestAsync(requestId);

                _logger.LogInformation("✅ الطلب {RequestId} تم قبوله وتعيينه للمندوب {CourierId}.", requestId, courierId);

                return Ok(new
                {
                    message = $"✅ تم قبول الطلب رقم {requestId} وتعيينه للمندوب رقم {courierId}.",
                    requestId,
                    courierId,
                    studentId = result.StudentId,
                    assignedTo = courierId,
                    updatedAt = DateTime.UtcNow
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
// 🎓 تحديث حالة الطلب من الطالب نفسه فقط (مع دعم تحديث ZoneId)
// ============================================================
[Authorize(Roles = "student,owner,admin")]
[HttpPut("{id:int}/student/status")]
public async Task<IActionResult> UpdateStatusByStudent(int id, [FromBody] UpdateMealRequestStatusDto dto)
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest(new { message = "⚠️ الحالة مطلوبة." });

        // 🎯 جلب الطلب الحالي
        var request = await _mealRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound(new { message = $"❌ الطلب بالمعرّف {id} غير موجود." });

        // 🧠 استخراج معرّف المستخدم الحالي من الـ Token
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        if (userIdClaim == null)
            return Unauthorized(new { message = "🚫 لم يتم العثور على معرّف المستخدم في التوكن." });

        // 🔒 التحقق من أن الطالب هو صاحب الطلب
        if (request.StudentId.ToString() != userIdClaim)
            return Forbid("🚫 لا يمكنك تعديل طلب ليس تابعًا لك.");

        // ⛔ التحقق من الحالات المسموح بها فقط
        var allowedStatuses = new[] { "pendingCourier", "cancelledByStudent" };
        if (!allowedStatuses.Contains(dto.Status))
            return BadRequest(new { message = "⚠️ لا يمكنك تعديل الحالة إلى هذه القيمة." });

        // 🚫 منع التعديل على طلب منتهي أو ملغى
        var lockedStatuses = new[] { "delivered", "cancelledByAdmin", "cancelledByCourier" };
        if (lockedStatuses.Contains(request.Status))
            return BadRequest(new { message = "🚫 لا يمكن تعديل حالة طلب منتهي أو ملغى." });

        // ✅ تحديث الحالة
        request.Status = dto.Status;
        request.UpdatedAt = DateTime.UtcNow;

        // 🗺️ إذا أرسل ZoneId من التطبيق، حدثه أيضًا
        if (dto.ZoneId.HasValue && dto.ZoneId.Value > 0)
        {
            request.ZoneId = dto.ZoneId.Value;
            _logger.LogInformation("📍 تم تحديث منطقة الطلب {Id} إلى ZoneId={ZoneId}", id, dto.ZoneId.Value);
        }

        await _mealRequestService.UpdateAsync(request);

        _logger.LogInformation("🎓 الطالب {StudentId} حدّث الطلب {Id} إلى الحالة {Status}",
            request.StudentId, id, dto.Status);

        return Ok(new
        {
            message = $"✅ تم تحديث حالة الطلب إلى {dto.Status}.",
            id,
            studentId = request.StudentId,
            newStatus = dto.Status,
            zoneId = request.ZoneId // ✅ إرجاع المنطقة بعد التحديث
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ فشل في تحديث حالة الطلب {Id} من قبل الطالب.", id);
        return StatusCode(500, new
        {
            message = "حدث خطأ أثناء تحديث حالة الطلب من الطالب.",
            details = ex.Message
        });
    }
}
        // ============================================================
        // 🔄 تحديث حالة الطلب (من المندوب أو الأدمن)
        // ============================================================
        [Authorize(Roles = "courier,owner,admin,student")]
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

    
}
