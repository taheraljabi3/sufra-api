using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Notifications;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Sufra.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly SufraDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(SufraDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // ➕ إنشاء إشعار جديد
        // ============================================================
        public async Task CreateAsync(NotificationDto dto)
        {
            try
            {
                var entity = new Notification
                {
                    UserId = dto.UserId,
                    Role = dto.Role,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedRequestId = dto.RelatedRequestId,
                    ZoneId = dto.ZoneId,
                    StudentId = dto.StudentId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsActive = true
                };

                _context.Notifications.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "🔔 تم إنشاء إشعار ({Role}) للمستخدم {UserId} في المنطقة {ZoneId}: {Title}",
                    dto.Role, dto.UserId, dto.ZoneId, dto.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل إنشاء إشعار للمستخدم {UserId}", dto.UserId);
                throw;
            }
        }

        // ============================================================
        // ➕ إنشاء إشعارات جماعية (Bulk Insert)
        // ============================================================
        public async Task CreateManyAsync(IEnumerable<NotificationDto> notifications)
        {
            try
            {
                if (notifications == null || !notifications.Any()) return;

                var entities = notifications.Select(dto => new Notification
                {
                    UserId = dto.UserId,
                    Role = dto.Role,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedRequestId = dto.RelatedRequestId,
                    ZoneId = dto.ZoneId,
                    StudentId = dto.StudentId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsActive = true
                }).ToList();

                _context.Notifications.AddRange(entities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("📢 تم إنشاء {Count} إشعارًا دفعة واحدة (ZoneId={ZoneId})",
                    entities.Count, entities.FirstOrDefault()?.ZoneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل إنشاء إشعارات جماعية.");
                throw;
            }
        }

        // ============================================================
        // 📬 جلب الإشعارات حسب المستخدم والدور
        // ============================================================
        public async Task<IEnumerable<NotificationDto>> GetByUserAsync(int userId, string role)
        {
            IQueryable<Notification> query;

            if (role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                query = _context.Notifications.Where(n => n.IsActive);
            }
            else
            {
                query = _context.Notifications.Where(
                    n => n.UserId == userId && n.Role.ToLower() == role.ToLower() && n.IsActive);
            }

            var results = await query
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            var relatedIds = results
                .Where(n => n.RelatedRequestId != null)
                .Select(n => n.RelatedRequestId.Value)
                .Distinct()
                .ToList();

            var requests = await _context.MealRequests
                .Where(r => relatedIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.AssignedCourierId);

            return results.Select(n =>
            {
                var dto = ToDto(n);
                if (n.Role?.ToLower() == "courier" && n.IsActive && n.RelatedRequestId != null)
                {
                    requests.TryGetValue(n.RelatedRequestId.Value, out var assignedCourierId);
                    dto.CanAccept = assignedCourierId == null || assignedCourierId == 0;
                }
                return dto;
            });
        }

        // ============================================================
        // 📫 جلب الإشعارات غير المقروءة فقط
        // ============================================================
        public async Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, string role)
        {
            IQueryable<Notification> query;

            if (role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                query = _context.Notifications.Where(n => !n.IsRead && n.IsActive);
            }
            else
            {
                query = _context.Notifications.Where(
                    n => n.UserId == userId &&
                         n.Role.ToLower() == role.ToLower() &&
                         !n.IsRead &&
                         n.IsActive);
            }

            var results = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return results.Select(ToDto);
        }

        // ============================================================
        // ✅ تحديد إشعار كمقروء + منطق الدور
        // ============================================================
        public async Task MarkAsReadAsync(int id)
        {
            var entity = await _context.Notifications.FindAsync(id);
            if (entity == null || entity.IsRead) return;

            entity.IsRead = true;
            if (entity.Role.Equals("student", StringComparison.OrdinalIgnoreCase))
                entity.IsActive = false;

            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("📖 تم تحديد الإشعار #{Id} كمقروء ({Role})", id, entity.Role);
        }

        // ============================================================
        // 🚫 تعطيل جميع الإشعارات المرتبطة بطلب محدد
        // ============================================================
        public async Task DeactivateByRequestAsync(int requestId)
        {
            try
            {
                var list = await _context.Notifications
                    .Where(n => n.RelatedRequestId == requestId && n.IsActive)
                    .ToListAsync();

                if (!list.Any()) return;

                foreach (var n in list)
                {
                    n.IsActive = false;
                    n.IsRead = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("🟡 تم تعطيل {Count} إشعار مرتبط بالطلب #{RequestId}.",
                    list.Count, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تعطيل الإشعارات للطلب #{RequestId}.", requestId);
                throw;
            }
        }

        // ============================================================
        // 🆕 جلب الإشعارات حسب المنطقة (بدون AutoMapper)
        // ============================================================
        public async Task<IEnumerable<NotificationDto>> GetByZoneAsync(int zoneId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.ZoneId == zoneId && n.IsActive);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var results = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();

            var relatedIds = results
                .Where(n => n.RelatedRequestId != null)
                .Select(n => n.RelatedRequestId.Value)
                .Distinct()
                .ToList();

            var requests = await _context.MealRequests
                .Where(r => relatedIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.AssignedCourierId);

            return results.Select(n =>
            {
                var dto = ToDto(n);
                if (n.Role?.ToLower() == "courier" && n.IsActive && n.RelatedRequestId != null)
                {
                    requests.TryGetValue(n.RelatedRequestId.Value, out var assignedCourierId);
                    dto.CanAccept = assignedCourierId == null || assignedCourierId == 0;
                }
                return dto;
            });
        }

        // ============================================================
        // 🗑️ حذف الإشعار نهائيًا
        // ============================================================
        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Notifications.FindAsync(id);
            if (entity == null) return;

            _context.Notifications.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🗑️ تم حذف الإشعار #{Id}.", id);
        }

        // ============================================================
        // 🧩 دالة مساعدة لتحويل الكيان إلى DTO
        // ============================================================
        private static NotificationDto ToDto(Notification n)
        {
            bool canAccept = false;
            if (n.Role?.ToLower() == "courier" && n.IsActive && n.RelatedRequestId != null)
                canAccept = true;

            return new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Role = n.Role,
                Title = n.Title,
                Message = n.Message,
                RelatedRequestId = n.RelatedRequestId,
                ZoneId = n.ZoneId,
                StudentId = n.StudentId,
                IsRead = n.IsRead,
                IsActive = n.IsActive,
                CreatedAt = n.CreatedAt,
                CanAccept = canAccept
            };
        }
    }
}
