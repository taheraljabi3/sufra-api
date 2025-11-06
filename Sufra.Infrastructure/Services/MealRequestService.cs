using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.DTOs.Couriers;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;

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
        // 🧾 جلب جميع الطلبات (مع العلاقات الكاملة)
        // ============================================================
        public async Task<IEnumerable<MealRequestDto>> GetAllAsync()
        {
            var mealRequests = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .Include(m => m.Subscription)
                .Include(m => m.AssignedCourier)
                    .ThenInclude(c => c.Student)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var result = mealRequests.Select(m => new MealRequestDto
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
                AssignedCourierId = m.AssignedCourierId,

                Student = m.Student != null ? new Sufra.Application.DTOs.Students.StudentDto
                {
                    Id = m.Student.Id,
                    UniversityId = m.Student.UniversityId,
                    Name = m.Student.Name,
                    Status = m.Student.Status
                } : null,

                Zone = m.Zone != null ? new Sufra.Application.DTOs.Zones.ZoneDto
                {
                    Id = m.Zone.Id,
                    Name = m.Zone.Name
                } : null,

                Subscription = m.Subscription != null ? new Sufra.Application.DTOs.Subscriptions.SubscriptionDto
                {
                    Id = m.Subscription.Id,
                    StartDate = m.Subscription.StartDate,
                    EndDate = m.Subscription.EndDate,
                    Status = m.Subscription.Status
                } : null,

                AssignedCourier = m.AssignedCourier != null ? new Sufra.Application.DTOs.Couriers.CourierDto
                {
                    Id = m.AssignedCourier.Id,
                    Name = m.AssignedCourier.Student?.Name ?? "—",
                    ZoneId = m.AssignedCourier.ZoneId
                } : null,

                StudentName = m.Student?.Name,
                ZoneName = m.Zone?.Name,
                CourierName = m.AssignedCourier?.Student?.Name
            });

            return result.ToList();
        }

        // ============================================================
        // 📦 إدخال دفعة وجبات (للأدمن فقط)
        // ============================================================
        public async Task<IEnumerable<MealRequestDto>> BulkCreateAsync(List<CreateMealRequestFullDto> requests)
        {
            if (requests == null || !requests.Any())
                throw new InvalidOperationException("⚠️ لا توجد وجبات للإدخال.");

            var entities = new List<MealRequest>();

            foreach (var dto in requests)
            {
                var entity = new MealRequest
                {
                    StudentId = dto.StudentId,
                    SubscriptionId = dto.SubscriptionId,
                    ZoneId = dto.ZoneId,
                    Period = dto.Period,
                    DeliveryType = dto.DeliveryType ?? "استلام ذاتي",
                    LocationDetails = dto.LocationDetails,
                    Notes = dto.Notes,
                    Status = dto.Status ?? "queued",
                    IsPaid = dto.IsPaid,
                    MealDate = DateTime.SpecifyKind(dto.MealDate.Date, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AssignedCourierId = dto.AssignedCourierId
                };

                entities.Add(entity);
            }

            await _context.MealRequests.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ تم إدخال {Count} وجبة دفعة واحدة بنجاح.", entities.Count);

            return entities.Select(ToDto).ToList();
        }
// ============================================================
// 🏗️ إنشاء وجبة كاملة (للأدمن فقط)
// ============================================================
public async Task<MealRequestDto> CreateAdminAsync(CreateMealRequestFullDto dto)
{
    var entity = new MealRequest
    {
        StudentId = dto.StudentId,
        SubscriptionId = dto.SubscriptionId,
        ZoneId = dto.ZoneId,
        Period = dto.Period,
        DeliveryType = dto.DeliveryType,
        LocationDetails = dto.LocationDetails,
        Notes = dto.Notes,
        Status = dto.Status ?? "queued",
        IsPaid = dto.IsPaid,
        MealDate = DateTime.SpecifyKind(dto.MealDate.Date, DateTimeKind.Utc),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ReqTime = DateTime.UtcNow,
        AssignedCourierId = dto.AssignedCourierId
    };

    _context.MealRequests.Add(entity);
    await _context.SaveChangesAsync();

    _logger.LogInformation("✅ تم إنشاء وجبة جديدة للطالب {StudentId} بتاريخ {MealDate} ({Period})",
        entity.StudentId, entity.MealDate.ToShortDateString(), entity.Period);

    return ToDto(entity);
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
                m.ZoneId == courierZone &&
                (m.Status == "queued" || m.Status == "waiting_for_courier" || m.Status == "on_the_way") &&
                m.AssignedCourierId == courierId)
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
                UniversityId = m.Student?.UniversityId ?? string.Empty,
                ZoneName = m.Zone?.Name ?? "غير محددة",
                
               // 🏠 إضافة رقم الغرفة من جدول StudentHousings
                RoomNo = _context.StudentHousings
                .Where(h => h.StudentId == m.StudentId && h.IsCurrent)
                .Select(h => h.RoomNo)
                .FirstOrDefault() ?? "غير محدد"
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

        // ============================================================
        // 🍱 إنشاء الطلب (للطلاب)
        // ============================================================
        public async Task<MealRequestDto> CreateAsync(CreateMealRequestDto dto)
        {
            var today = DateTime.UtcNow.Date;

            // 🛑 منع التكرار لنفس اليوم والفترة
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

            // 📍 جلب ZoneId الحقيقي من السكن
            int resolvedZoneId;
            var housing = await _context.Set<StudentHousing>()
                .FirstOrDefaultAsync(h => h.StudentId == dto.StudentId);

            if (housing != null)
            {
                resolvedZoneId = housing.ZoneId;
                _logger.LogInformation("📍 ZoneId مأخوذ من السكن للطالب {StudentId}: {ZoneId}", dto.StudentId, resolvedZoneId);
            }
            else
            {
                resolvedZoneId = dto.ZoneId;
                _logger.LogWarning("⚠️ لم يتم العثور على سكن للطالب {StudentId}. استخدام ZoneId القادم ({ZoneId}).",
                    dto.StudentId, resolvedZoneId);
            }

            var mealRequest = new MealRequest
            {
                StudentId = dto.StudentId,
                ZoneId = resolvedZoneId,
                SubscriptionId = dto.SubscriptionId ?? 0,
                Period = dto.Period,
                DeliveryType = dto.DeliveryType ?? "استلام ذاتي",
                LocationDetails = dto.LocationDetails,
                Notes = dto.Notes,
                Status = (dto.DeliveryType?.ToLower() == "توصيل" || dto.DeliveryType?.ToLower() == "delivery")
                    ? "waiting_for_courier"
                    : "queued",
                MealDate = today,
                CreatedAt = DateTime.UtcNow,
                IsPaid = true
            };

            _context.MealRequests.Add(mealRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ تم إنشاء الطلب الجديد {Id} للطالب {StudentId} في Zone={ZoneId} ({Period})",
                mealRequest.Id, mealRequest.StudentId, mealRequest.ZoneId, mealRequest.Period);

            // إشعارات المندوبين والطالب
            if (mealRequest.DeliveryType?.ToLower() == "توصيل" || mealRequest.DeliveryType?.ToLower() == "delivery")
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
                        RelatedRequestId = mealRequest.Id,
                        ZoneId = mealRequest.ZoneId
                    }).ToList();

                    await _notificationService.CreateManyAsync(notifications);
                    _logger.LogInformation("✅ تم إشعار {Count} مندوبيْن في Zone={ZoneId}.", couriers.Count, mealRequest.ZoneId);
                }

                await _notificationService.CreateAsync(new NotificationDto
                {
                    UserId = mealRequest.StudentId,
                    Role = "student",
                    Title = "✅ تم إرسال طلبك",
                    Message = $"تم إرسال طلب {dto.Period} إلى المندوبين في منطقتك.",
                    RelatedRequestId = mealRequest.Id,
                    ZoneId = mealRequest.ZoneId
                });
            }

            return ToDto(mealRequest);
        }

        // ============================================================
        // 📢 تحديث الطلب الحالي وإشعار المندوبين والطالب
        // ============================================================
        public async Task<MealRequestDto?> NotifyCouriersOnlyAsync(CreateMealRequestDto dto)
        {
            var today = DateTime.UtcNow.Date;

            var existing = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .FirstOrDefaultAsync(m =>
                    m.StudentId == dto.StudentId &&
                    m.Period == dto.Period &&
                    m.MealDate.Date == today);

            if (existing == null)
            {
                _logger.LogWarning("⚠️ لا يوجد طلب مطابق للطالب {StudentId} في {Date} ({Period})",
                    dto.StudentId, today.ToString("yyyy-MM-dd"), dto.Period);
                return null;
            }

            if (dto.ZoneId > 0 && existing.ZoneId != dto.ZoneId)
            {
                existing.ZoneId = dto.ZoneId;
                _logger.LogInformation("📍 تحديث ZoneId للطلب {Id} إلى {ZoneId}", existing.Id, dto.ZoneId);
            }

            if (!string.IsNullOrWhiteSpace(dto.LocationDetails))
            {
                existing.LocationDetails = dto.LocationDetails;
                _logger.LogInformation("🏠 تحديث LocationDetails للطلب {Id}", existing.Id);
            }

            existing.Status = "queued";
            existing.UpdatedAt = DateTime.UtcNow;

            // إشعارات
            var couriers = await _context.Couriers
                .Include(c => c.Student)
                .Where(c => c.ZoneId == existing.ZoneId && c.Student.Status == "active")
                .ToListAsync();

            if (couriers.Any())
            {
                var notifications = couriers.Select(c => new NotificationDto
                {
                    UserId = c.Id,
                    Role = "courier",
                    Title = "📦 طلب توصيل جديد في منطقتك",
                    Message = $"الطالب {existing.Student?.Name ?? "مجهول"} طلب وجبة {existing.Period} في منطقتك.",
                    RelatedRequestId = existing.Id,
                    ZoneId = existing.ZoneId
                }).ToList();

                await _notificationService.CreateManyAsync(notifications);
                _logger.LogInformation("📢 إرسال {Count} إشعارات للمندوبين في Zone={ZoneId}",
                    notifications.Count, existing.ZoneId);
            }

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = existing.StudentId,
                Role = "student",
                Title = "✅ تم إرسال طلبك",
                Message = $"تم إرسال طلب {existing.Period} إلى المندوبين في منطقتك ({existing.Zone?.Name ?? "غير معروفة"}).",
                RelatedRequestId = existing.Id,
                ZoneId = existing.ZoneId
            });

            await _context.SaveChangesAsync();
            return ToDto(existing);
        }

        // ============================================================
        // 🚴‍♂️ قبول الطلب من المندوب
        // ============================================================
        public async Task<(bool Success, string Message, int StudentId)> AssignCourierAsync(int requestId, int courierId)
        {
            var meal = await _context.MealRequests
                .Include(m => m.Student)
                .Include(m => m.Zone)
                .FirstOrDefaultAsync(m => m.Id == requestId);

            if (meal == null)
                return (false, "❌ لا يمكن قبول الطلب لأنه غير موجود.", 0);

            if (meal.AssignedCourierId != null)
                return (false, "⚠️ تم قبول هذا الطلب من قبل مندوب آخر.", meal.StudentId);

            var courier = await _context.Couriers
                .Include(c => c.Student)
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == courierId);

            if (courier == null)
                return (false, "❌ لم يتم العثور على بيانات المندوب.", meal.StudentId);

            if (meal.ZoneId != courier.ZoneId)
                return (false, $"🚫 لا يمكنك قبول الطلب لأنه في منطقة مختلفة ({meal.Zone?.Name ?? "غير معروفة"}).", meal.StudentId);

            meal.AssignedCourierId = courier.Id;
            meal.Status = "on_the_way";
            meal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = meal.StudentId,
                Role = "student",
                Title = "🚴‍♂️ تم قبول طلبك",
                Message = $"تم قبول طلب {meal.Period} من المندوب {courier.Student?.Name ?? "مندوب"} ({meal.Zone?.Name}).",
                RelatedRequestId = meal.Id,
                ZoneId = meal.ZoneId
            });

            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = courier.StudentId,
                Role = "courier",
                Title = "✅ تم إسناد الطلب إليك",
                Message = $"تم تعيين الطلب رقم {meal.Id} ({meal.Period}) لك للتوصيل.",
                RelatedRequestId = meal.Id,
                ZoneId = meal.ZoneId
            });

            return (true, $"✅ تم قبول الطلب وإسناده بنجاح إلى {courier.Student?.Name}.", meal.StudentId);
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
                RelatedRequestId = entity.Id,
                ZoneId = entity.ZoneId
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
