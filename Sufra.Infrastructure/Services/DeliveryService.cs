using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Deliveries;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;
using Sufra.Application.DTOs.Couriers;

namespace Sufra.Infrastructure.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public DeliveryService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
public async Task<IEnumerable<DeliveryProofDto>> GetByCourierAsync(int courierId)
{
    // ✅ جلب بيانات المندوب
    var courier = await _context.Couriers
        .Include(c => c.Student)
        .FirstOrDefaultAsync(c => c.Id == courierId);

    if (courier == null)
        throw new InvalidOperationException("❌ المندوب غير موجود.");

    if (courier.ZoneId == 0)
        throw new InvalidOperationException("⚠️ لم يتم تحديد منطقة لهذا المندوب.");

    // ✅ جلب الطلبات الخاصة بمنطقة المندوب ونوع "توصيل"
    var requests = await _context.MealRequests
        .Include(r => r.Student)
        .Include(r => r.Zone)
        .Where(r => r.ZoneId == courier.ZoneId && r.DeliveryType == "توصيل")
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    // ✅ جلب بيانات السكن لكل طالب
    var studentIds = requests.Select(r => r.StudentId).Distinct().ToList();
    var housings = await _context.StudentHousings
        .Where(h => studentIds.Contains(h.StudentId) && h.IsCurrent)
        .ToListAsync();

    // 🧠 الدمج الذكي بين الطلبات والسكن
    var result = requests.Select(r =>
    {
        var housing = housings.FirstOrDefault(h => h.StudentId == r.StudentId);

        return new DeliveryProofDto
        {
            Id = r.Id,
            MealRequestId = r.Id,
            CourierId = courier.Id,
            CourierName = courier.Student?.Name ?? "—",
            StudentName = r.Student?.Name ?? "غير معروف",
            ZoneName = housing?.ZoneName ?? r.Zone?.Name ?? "—",
            RoomNo = housing?.RoomNo ?? "—",
            Notes = $"📦 مهمة من المنطقة {(housing?.ZoneName ?? r.Zone?.Name ?? "غير محددة")} - الغرفة {(housing?.RoomNo ?? "—")}",
            Status = r.Status,
            DeliveredAt = null
        };
    }).ToList();

    return result;
}
// ============================================================
// 🟧 2️⃣ جلب جميع عمليات التوصيل (للأدمن) مع السكن
// ============================================================
public async Task<IEnumerable<DeliveryProofDto>> GetAllAsync()
{
    var deliveries = await _context.DeliveryProofs
        .Include(d => d.MealRequest)
            .ThenInclude(r => r.Student)
        .Include(d => d.MealRequest.Zone)
        .Include(d => d.Courier)
        .OrderByDescending(d => d.DeliveredAt)
        .ToListAsync();

    // 🏠 جلب بيانات السكن الحالية لجميع الطلبة في عمليات التوصيل
    var studentIds = deliveries
        .Where(d => d.MealRequest != null)
        .Select(d => d.MealRequest.StudentId)
        .Distinct()
        .ToList();

    var housings = await _context.StudentHousings
        .Where(h => studentIds.Contains(h.StudentId) && h.IsCurrent)
        .ToListAsync();

    // 🔁 دمج البيانات مع السكن
    var result = deliveries.Select(d =>
    {
        var housing = housings.FirstOrDefault(h => h.StudentId == d.MealRequest.StudentId);
        return new DeliveryProofDto
        {
            Id = d.Id,
            MealRequestId = d.MealRequestId,
            CourierId = d.CourierId,
            CourierName = d.Courier?.Student?.Name ?? "—",
            StudentName = d.MealRequest?.Student?.Name ?? "غير معروف",
            ZoneName = d.MealRequest?.Zone?.Name ?? housing?.ZoneName ?? "—",
            RoomNo = housing?.RoomNo ?? "—",
            Notes = d.Notes,
            Status = d.MealRequest?.Status ?? "—",
            DeliveredAt = d.DeliveredAt
        };
    }).ToList();

    return result;
}

        // ============================================================
        // 🟩 3️⃣ تأكيد عملية التسليم من المندوب
        // ============================================================
        public async Task<DeliveryProofDto> ConfirmDeliveryAsync(CreateDeliveryProofDto dto)
        {
            var request = await _context.MealRequests
                .Include(r => r.Subscription)
                .FirstOrDefaultAsync(r => r.Id == dto.MealRequestId);

            if (request == null)
                throw new InvalidOperationException("⚠️ الطلب غير موجود.");

            if (request.Status == "تم التسليم")
                throw new InvalidOperationException("✅ الطلب تم تسليمه مسبقًا.");

            var proof = _mapper.Map<DeliveryProof>(dto);
            proof.DeliveredAt = DateTime.UtcNow;
            proof.Status = "تم التسليم";

            request.Status = "تم التسليم";

            // 🔁 تحديث حالة الدفعة إن اكتملت
            var batchItem = await _context.BatchItems.FirstOrDefaultAsync(b => b.ReqId == request.Id);
            if (batchItem != null)
            {
                var allDelivered = await _context.BatchItems
                    .Where(b => b.BatchId == batchItem.BatchId)
                    .AllAsync(b => b.MealRequest.Status == "تم التسليم");

                if (allDelivered)
                {
                    var batch = await _context.Batches.FindAsync(batchItem.BatchId);
                    if (batch != null)
                        batch.Status = "مكتمل";
                }
            }

            _context.DeliveryProofs.Add(proof);
            await _context.SaveChangesAsync();

            return _mapper.Map<DeliveryProofDto>(proof);
        }

        // ============================================================
        // 🟨 4️⃣ التعيين التلقائي للطلب الجديد إلى مندوب
        // ============================================================
        public async Task AssignToCourierAsync(MealRequestDto mealRequest)
        {
            // نجلب الطلب نفسه من قاعدة البيانات للحصول على ZoneId
            var requestEntity = await _context.MealRequests
                .FirstOrDefaultAsync(r => r.Id == mealRequest.Id);

            if (requestEntity == null)
                throw new InvalidOperationException("❌ الطلب غير موجود.");

            // البحث عن أول مندوب نشط في نفس المنطقة
            var courier = await _context.Couriers
                .Where(c => c.ZoneId == requestEntity.ZoneId && c.Status == "active")
                .OrderBy(c => c.JoinedAt)
                .FirstOrDefaultAsync();

            if (courier == null)
                throw new InvalidOperationException("🚫 لا يوجد مندوب متاح في هذه المنطقة حاليًا.");

            // إنشاء سجل مهمة جديدة (DeliveryProof)
            var delivery = new DeliveryProof
            {
                MealRequestId = mealRequest.Id,
                CourierId = courier.Id,
                Status = "قيد التوصيل",
                Notes = "تم إنشاء المهمة تلقائيًا من النظام",
                IsVerified = false,
                EvidenceUrl = null
            };

            _context.DeliveryProofs.Add(delivery);
            await _context.SaveChangesAsync();
        }

        // ============================================================
        // 🟦 5️⃣ جلب المندوبين المرتبطين بمنطقة معينة (Zone)
        // ============================================================
        public async Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId)
        {
            var couriers = await _context.Couriers
                .Include(c => c.Student)
                .Where(c => c.ZoneId == zoneId)
                .Select(c => new CourierDto
                {
                    Id = c.Id,
                    Name = c.Student.Name,
                    Phone = c.Student.Phone ?? "—",
                    ZoneId = c.ZoneId
                })
                .ToListAsync();

            return couriers;
        }
    }
}
