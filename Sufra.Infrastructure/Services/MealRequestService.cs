using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Sufra.Application.DTOs.Couriers;

namespace Sufra.Application.Services
{
    public class MealRequestService : IMealRequestService
    {
        private readonly SufraDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MealRequestService> _logger;

        public MealRequestService(
            SufraDbContext context,
            INotificationService notificationService,
            ILogger<MealRequestService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // 🧾 جلب جميع الطلبات
        // ============================================================
        public async Task<IEnumerable<MealRequestDto>> GetAllAsync()
        {
            var query = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .Include(m => m.Subscription)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return query.Select(ToDto);
        }

        // ============================================================
        // 🧍‍♂️ جلب الطلبات حسب الطالب
        // ============================================================
        public async Task<IEnumerable<MealRequestDto>> GetByStudentAsync(int studentId)
        {
            var query = await _context.MealRequests
                .Include(m => m.Zone)
                .Include(m => m.Subscription)
                .Where(m => m.StudentId == studentId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return query.Select(ToDto);
        }

        // ============================================================
        // 🚴‍♂️ جلب الطلبات الخاصة بالمندوب
        // ============================================================
        public async Task<IEnumerable<MealRequestDto>> GetByCourierAsync(int courierId)
        {
            var courier = await _context.Couriers
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == courierId);

            if (courier == null)
                throw new InvalidOperationException("🚫 لم يتم العثور على بيانات المندوب.");

            var courierZone = courier.ZoneId;

            var query = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .Where(m =>
                    (m.ZoneId == courierZone &&
                     (m.Status == "waiting_for_courier" ||
                      m.Status == "queued" ||
                      m.Status == "assigned" ||
                      m.Status == "on_the_way") &&
                     m.AssignedCourierId == null)
                    ||
                    (m.AssignedCourierId == courierId)
                )
                .OrderBy(m => m.Status == "queued" ? 0 : 1)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();

            return query.Select(m => new MealRequestDto
            {
                Id = m.Id,
                StudentId = m.StudentId,
                SubscriptionId = m.SubscriptionId,
                ZoneId = m.ZoneId,
                Period = m.Period,
                ReqTime = m.ReqTime,
                DeliveryType = m.DeliveryType,
                LocationDetails = m.LocationDetails,
                Notes = m.Notes,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                MealDate = m.MealDate,
                IsPaid = m.IsPaid,
                StudentName = m.Student?.Name ?? "غير معروف",
                ZoneName = m.Zone?.Name ?? "غير محددة"
            });
        }

        // ============================================================
        // 🔍 جلب طلب واحد
        // ============================================================
        public async Task<MealRequestDto?> GetByIdAsync(int id)
        {
            var meal = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .FirstOrDefaultAsync(m => m.Id == id);

            return meal == null ? null : ToDto(meal);
        }
public async Task<MealRequestDto> CreateAsync(CreateMealRequestDto dto)
{
    // 🗓️ تحديد تاريخ اليوم (بدون وقت)
    var today = DateTime.UtcNow.Date;

    // 🔍 منع التكرار: لا يُسمح بإنشاء أكثر من طلب لنفس الطالب والفترة في نفس اليوم
    var existing = await _context.MealRequests
        .FirstOrDefaultAsync(m =>
            m.StudentId == dto.StudentId &&
            m.Period == dto.Period &&
            m.MealDate == today);

    if (existing != null)
    {
        _logger.LogWarning("⚠️ يوجد طلب مسبق لنفس الطالب ({StudentId}) والفترة ({Period}) في اليوم ({Date}).",
            dto.StudentId, dto.Period, today.ToString("yyyy-MM-dd"));
        throw new InvalidOperationException($"يوجد طلب سابق لهذه الفترة ({existing.Status}).");
    }

    // 🧭 تحديد المنطقة تلقائيًا من السكن إذا لم تُرسل
    int zoneId = dto.ZoneId;
    if (zoneId == 0)
    {
        var housing = await _context.Set<StudentHousing>()
            .FirstOrDefaultAsync(h => h.StudentId == dto.StudentId);
        if (housing != null)
        {
            zoneId = housing.ZoneId;
            _logger.LogInformation("📍 تم جلب ZoneId تلقائيًا من السكن: {ZoneId}", zoneId);
        }
        else
        {
            _logger.LogWarning("⚠️ لم يتم العثور على بيانات السكن للطالب {StudentId}.", dto.StudentId);
        }
    }

    // 🍱 إنشاء الطلب الجديد
    var mealRequest = new MealRequest
    {
        StudentId = dto.StudentId,
        ZoneId = zoneId,
        SubscriptionId = dto.SubscriptionId ?? 0,
        Period = dto.Period,
        DeliveryType = dto.DeliveryType ?? "استلام ذاتي",
        LocationDetails = dto.LocationDetails,
        Notes = dto.Notes,
        Status = dto.DeliveryType == "توصيل" ? "waiting_for_courier" : "queued",
        MealDate = today,
        CreatedAt = DateTime.UtcNow,
        IsPaid = true
    };

    _context.MealRequests.Add(mealRequest);
    await _context.SaveChangesAsync();

    _logger.LogInformation("✅ تم إنشاء الطلب الجديد بالمعرّف {Id} للطالب {StudentId} ({Period})",
        mealRequest.Id, mealRequest.StudentId, mealRequest.Period);

    // 🚴‍♂️ إشعار المندوبين في نفس المنطقة إذا كان توصيل
    if (dto.DeliveryType == "توصيل")
    {
        var couriers = await _context.Couriers
            .Include(c => c.Student)
            .Where(c => c.ZoneId == mealRequest.ZoneId && c.Student.Status == "active")
            .ToListAsync();

        if (couriers.Any())
        {
            var notifications = couriers.Select(c => new NotificationDto
            {
                UserId = c.StudentId,
                Role = "courier",
                Title = "📦 طلب جديد في منطقتك",
                Message = $"الطالب #{dto.StudentId} طلب وجبة {dto.Period} جديدة في منطقتك، بانتظار القبول.",
                RelatedRequestId = mealRequest.Id
            }).ToList();

            await _notificationService.CreateManyAsync(notifications);

            _logger.LogInformation("✅ تم إشعار {Count} مندوبيْن في المنطقة {ZoneId}.",
                couriers.Count, mealRequest.ZoneId);
        }
        else
        {
            _logger.LogWarning("⚠️ لا يوجد مندوبين نشطين في المنطقة (ZoneId={ZoneId})", mealRequest.ZoneId);
        }
    }

    // ✅ إرجاع الطلب الجديد
    return new MealRequestDto
    {
        Id = mealRequest.Id,
        StudentId = mealRequest.StudentId,
        ZoneId = mealRequest.ZoneId,
        SubscriptionId = mealRequest.SubscriptionId,
        Period = mealRequest.Period,
        DeliveryType = mealRequest.DeliveryType,
        LocationDetails = mealRequest.LocationDetails,
        Notes = mealRequest.Notes,
        Status = mealRequest.Status,
        MealDate = mealRequest.MealDate,
        CreatedAt = mealRequest.CreatedAt,
        IsPaid = mealRequest.IsPaid
    };
}

// ============================================================
// 📢 تحديث الطلب الحالي + إشعار المندوبين في نفس المنطقة
// ============================================================
public async Task<MealRequestDto?> NotifyCouriersOnlyAsync(CreateMealRequestDto dto)
{
    var today = DateTime.UtcNow.Date;

    // 🔍 1️⃣ البحث عن الطلب الحالي للطالب في نفس اليوم والفترة
    var existing = await _context.MealRequests
        .Include(m => m.Student)
        .Include(m => m.Zone)
        .FirstOrDefaultAsync(m =>
            m.StudentId == dto.StudentId &&
            m.Period == dto.Period &&
            m.MealDate.Date == today);

    if (existing == null)
    {
        _logger.LogWarning("⚠️ لم يتم العثور على طلب للطالب {StudentId} في الفترة {Period} بتاريخ {Date}.",
            dto.StudentId, dto.Period, today.ToString("yyyy-MM-dd"));
        return null;
    }

    // 🟡 2️⃣ تحديث الحالة إلى 'queued' إذا لم تكن كذلك
    if (existing.Status != "queued")
    {
        existing.Status = "queued";
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ تم تحديث حالة الطلب {Id} للطالب {StudentId} إلى 'queued'.",
            existing.Id, existing.StudentId);
    }
    else
    {
        _logger.LogInformation("ℹ️ الطلب {Id} للطالب {StudentId} هو بالفعل في حالة 'queued'.",
            existing.Id, existing.StudentId);
    }

    // 🚴‍♂️ 3️⃣ جلب المندوبين في نفس المنطقة
    var couriers = await _context.Couriers
        .Include(c => c.Student)
        .Where(c => c.ZoneId == existing.ZoneId && c.Student.Status == "active")
        .ToListAsync();

    if (couriers.Any())
    {
        // 🔔 4️⃣ إنشاء الإشعارات للمندوبين
        var notifications = couriers.Select(c => new NotificationDto
        {
            UserId = c.StudentId,
            Role = "courier",
            Title = "📦 طلب توصيل جديد في منطقتك",
            Message = $"الطالب {existing.Student?.Name ?? "مجهول"} طلب وجبة {existing.Period} بانتظار القبول.",
            RelatedRequestId = existing.Id
        }).ToList();

        await _notificationService.CreateManyAsync(notifications);

        _logger.LogInformation("✅ تم إشعار {Count} مندوبيْن في المنطقة {ZoneId} بخصوص الطلب {RequestId}.",
            couriers.Count, existing.ZoneId, existing.Id);
    }
    else
    {
        _logger.LogWarning("⚠️ لا يوجد مندوبين نشطين في المنطقة (ZoneId={ZoneId}).", existing.ZoneId);
    }

    // 📢 5️⃣ إشعار الطالب بأن طلبه أُرسل
    await _notificationService.CreateAsync(new NotificationDto
    {
        UserId = existing.StudentId,
        Role = "student",
        Title = "✅ تم إرسال طلبك",
        Message = $"تم إرسال طلب {existing.Period} إلى المندوبين في منطقتك بانتظار قبول أحدهم.",
        RelatedRequestId = existing.Id
    });

    // 🧩 6️⃣ إرجاع بيانات الطلب بعد التحديث
    return new MealRequestDto
    {
        Id = existing.Id,
        StudentId = existing.StudentId,
        ZoneId = existing.ZoneId,
        Period = existing.Period,
        DeliveryType = existing.DeliveryType,
        LocationDetails = existing.LocationDetails,
        Notes = existing.Notes,
        Status = existing.Status,
        MealDate = existing.MealDate,
        CreatedAt = existing.CreatedAt,
        UpdatedAt = existing.UpdatedAt,
        IsPaid = existing.IsPaid
    };
}
        // ============================================================
        // 🚴‍♂️ قبول الطلب من أحد المندوبين
        // ============================================================
        public async Task<(bool Success, string Message, int StudentId)> AssignCourierAsync(int requestId, int courierId)
        {
            var meal = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .FirstOrDefaultAsync(m => m.Id == requestId);

            if (meal == null)
                return (false, "❌ لا يمكن قبول الطلب لأنه غير موجود في النظام.", 0);

            if (meal.AssignedCourierId != null)
                return (false, "⚠️ تم قبول هذا الطلب بالفعل من قِبل مندوب آخر.", meal.StudentId);

            var courier = await _context.Couriers
                .Include(c => c.Student)
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == courierId);

            if (courier == null)
                return (false, "❌ لم يتم العثور على بيانات المندوب.", meal.StudentId);

            if (meal.ZoneId != courier.ZoneId)
                return (false, $"🚫 لا يمكنك قبول هذا الطلب لأنه في منطقة مختلفة ({meal.Zone?.Name ?? "غير معروفة"}).", meal.StudentId);

            var courierName = courier.Student?.Name ?? "مندوب غير معروف";
            var studentName = meal.Student?.Name ?? "طالب غير معروف";
            var zoneName = meal.Zone?.Name ?? "منطقة غير معروفة";

            meal.AssignedCourierId = courier.Id;
            meal.Status = "on_the_way";
            meal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = meal.StudentId,
                Role = "student",
                Title = "🚴‍♂️ تم قبول طلبك",
                Message = $"تم قبول طلب {meal.Period} من المندوب {courierName} ({zoneName}) وسيتم توصيله قريبًا بإذن الله.",
                RelatedRequestId = meal.Id
            });

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = courier.StudentId,
                Role = "courier",
                Title = "✅ تم إسناد الطلب إليك رسميًا",
                Message = $"تم تعيين الطلب رقم {meal.Id} ({meal.Period}) لك للتوصيل إلى الطالب {studentName}.",
                RelatedRequestId = meal.Id
            });

            var owners = await _context.Students
                .Where(s => s.Role == "owner")
                .ToListAsync();

            if (owners.Any())
            {
                var ownerNotifications = owners.Select(o => new NotificationDto
                {
                    UserId = o.Id,
                    Role = "owner",
                    Title = "📦 عملية قبول طلب جديدة",
                    Message = $"📢 المندوب {courierName} قبل الطلب رقم {meal.Id} ({meal.Period}) للطالب {studentName} في {zoneName}.",
                    RelatedRequestId = meal.Id
                });

                await _notificationService.CreateManyAsync(ownerNotifications);
            }

            _logger.LogInformation("✅ المندوب {CourierName} (ID={CourierId}) قبل الطلب {RequestId} في المنطقة {ZoneName}.",
                courierName, courierId, requestId, zoneName);

            return (true, $"✅ تم قبول الطلب وإسناده بنجاح إلى المندوب {courierName}.", meal.StudentId);
        }

        // ============================================================
        // 🔄 تحديث حالة الطلب
        // ============================================================
        public async Task<MealRequestDto?> UpdateAsync(MealRequestDto dto)
        {
            var entity = await _context.MealRequests.FindAsync(dto.Id);
            if (entity == null)
                throw new InvalidOperationException($"❌ الطلب رقم {dto.Id} غير موجود.");

            entity.Status = dto.Status;
            entity.Notes = dto.Notes;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = entity.StudentId,
                Role = "student",
                Title = "🔔 تحديث حالة الطلب",
                Message = $"تم تغيير حالة وجبة {entity.Period} إلى {entity.Status}",
                RelatedRequestId = entity.Id
            });

            return ToDto(entity);
        }

        // ============================================================
        // 🗑️ حذف الطلب
        // ============================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.MealRequests.FindAsync(id);
            if (entity == null) return false;

            _context.MealRequests.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // 🧭 جلب جميع المندوبين في منطقة محددة
        // ============================================================
        public async Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId)
        {
            return await _context.Couriers
                .Include(c => c.Student)
                .Where(c => c.ZoneId == zoneId && c.Student.Status == "active")
                .Select(c => new CourierDto
                {
                    Id = c.Id,
                    Name = c.Student.Name,
                    Phone = c.Student.Phone ?? "—",
                    ZoneId = c.ZoneId
                })
                .ToListAsync();
        }

        // ============================================================
        // 🧩 تحويل إلى DTO
        // ============================================================
        private static MealRequestDto ToDto(MealRequest m) => new()
        {
            Id = m.Id,
            StudentId = m.StudentId,
            ZoneId = m.ZoneId,
            SubscriptionId = m.SubscriptionId,
            Period = m.Period,
            ReqTime = m.ReqTime,
            DeliveryType = m.DeliveryType,
            LocationDetails = m.LocationDetails,
            Notes = m.Notes,
            Status = m.Status,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            MealDate = m.MealDate,
            IsPaid = m.IsPaid,
            AssignedCourierId = m.AssignedCourierId
        };
    }
}
