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
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🔔 تم إنشاء إشعار ({Role}) للمستخدم {UserId}: {Title}",
                    dto.Role, dto.UserId, dto.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل إنشاء إشعار للمستخدم {UserId}", dto.UserId);
                throw;
            }
        }

        // ============================================================
        // ➕ إنشاء إشعارات جماعية (لعدة مستخدمين دفعة واحدة)
        // ============================================================
        public async Task CreateManyAsync(IEnumerable<NotificationDto> notifications)
        {
            try
            {
                var entities = notifications.Select(dto => new Notification
                {
                    UserId = dto.UserId,
                    Role = dto.Role,
                    Title = dto.Title,
                    Message = dto.Message,
                    RelatedRequestId = dto.RelatedRequestId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });

                _context.Notifications.AddRange(entities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🔔 تم إنشاء {Count} إشعار دفعة واحدة.", notifications.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل إنشاء إشعارات جماعية.");
                throw;
            }
        }

        // ============================================================
        // 📬 جلب الإشعارات حسب المستخدم (مرتبة حسب الحالة والوقت)
        // ============================================================
public async Task<IEnumerable<NotificationDto>> GetByUserAsync(int userId, string role)
{
    IQueryable<Notification> query;

    // 👑 إذا المستخدم مالك (Owner) يشوف كل الإشعارات بدون فلترة
    if (role.Equals("owner", StringComparison.OrdinalIgnoreCase))
    {
        query = _context.Notifications;
    }
    else
    {
        query = _context.Notifications.Where(n => n.UserId == userId && n.Role == role);
    }

    var results = await query
        .OrderBy(n => n.IsRead)            // ⬅️ غير المقروء أولًا
        .ThenByDescending(n => n.CreatedAt)
        .ToListAsync();

    return results.Select(n => new NotificationDto
    {
        Id = n.Id,
        UserId = n.UserId,
        Role = n.Role,
        Title = n.Title,
        Message = n.Message,
        RelatedRequestId = n.RelatedRequestId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    });
}
        // ============================================================
        // 📫 جلب الإشعارات غير المقروءة فقط
        // ============================================================
        public async Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, string role)
{
    IQueryable<Notification> query;

    // 👑 المالك (Owner) يشوف جميع الإشعارات غير المقروءة في النظام
    if (role.Equals("owner", StringComparison.OrdinalIgnoreCase))
    {
        query = _context.Notifications.Where(n => !n.IsRead);
    }
    else
    {
        query = _context.Notifications
            .Where(n => n.UserId == userId && n.Role == role && !n.IsRead);
    }

    var results = await query
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return results.Select(n => new NotificationDto
    {
        Id = n.Id,
        UserId = n.UserId,
        Role = n.Role,
        Title = n.Title,
        Message = n.Message,
        RelatedRequestId = n.RelatedRequestId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    });
}

        // ============================================================
        // ✅ تحديد إشعار كمقروء (مع حماية من التكرار)
        // ============================================================
        public async Task MarkAsReadAsync(int id)
        {
            var entity = await _context.Notifications.FindAsync(id);
            if (entity == null || entity.IsRead) return;

            entity.IsRead = true;
            _context.Entry(entity).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // 🗑️ حذف الإشعار
        // ============================================================
        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Notifications.FindAsync(id);
            if (entity != null)
            {
                _context.Notifications.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        
    }
}
