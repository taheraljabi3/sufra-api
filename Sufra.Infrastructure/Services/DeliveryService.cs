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

        // ============================================================
        // 🟦 1️⃣ جلب جميع مهام المندوب حسب المعرّف
        // ============================================================
        public async Task<IEnumerable<DeliveryProofDto>> GetByCourierAsync(int courierId)
        {
            // 1️⃣ احصل على منطقة المندوب
            var courierZoneId = await _context.Couriers
                .Where(c => c.Id == courierId)
                .Select(c => c.ZoneId)
                .FirstOrDefaultAsync();

            if (courierZoneId == 0)
                throw new InvalidOperationException("⚠️ لم يتم تحديد منطقة لهذا المندوب.");

            // 2️⃣ جلب جميع الطلبات ضمن نفس المنطقة من نوع توصيل ولم تُسلَّم بعد
            var requests = await _context.MealRequests
                .Include(r => r.Subscription)
                .Include(r => r.Zone)
                .Where(r => r.ZoneId == courierZoneId
                            && r.DeliveryType == "توصيل"
                            && r.Status != "تم التسليم"
                            && r.Status != "ملغى")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 3️⃣ تحويل الطلبات إلى DeliveryProofDto لعرضها كمهام للمندوب
            var tasks = requests.Select(r => new DeliveryProofDto
            {
                MealRequestId = r.Id,
                CourierId = courierId,
                Status = r.Status,
                // ❌ حذف DeliveredAt = null (القيمة الافتراضية تكفي)
                Notes = $"📦 مهمة منطقية من المنطقة {r.Zone?.Name ?? r.ZoneId.ToString()}",
                // ❌ لا يوجد MealRequest في DTO، لذا نضيف التفاصيل في حقل جديد
            }).ToList();

            return tasks;
        }
        public async Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId)
        {
            return await _context.Couriers
                .Include(c => c.Student)
                .Where(c => c.ZoneId == zoneId)
                .Select(c => new CourierDto
                {
                    Id = c.Id,
                    Name = c.Student.Name,              // 🔹 اسم المندوب من الطالب
                    Phone = c.Student.Phone ?? "—",     // 🔹 رقم الجوال من الطالب
                    ZoneId = c.ZoneId
                })
                .ToListAsync();
        }

        // ============================================================
        // 🟧 2️⃣ جلب جميع عمليات التوصيل (للأدمن)
        // ============================================================
        public async Task<IEnumerable<DeliveryProofDto>> GetAllAsync()
        {
            var deliveries = await _context.DeliveryProofs
                .Include(d => d.MealRequest)
                    .ThenInclude(r => r.Subscription)
                .Include(d => d.MealRequest.Zone)
                .Include(d => d.Courier)
                .OrderByDescending(d => d.DeliveredAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DeliveryProofDto>>(deliveries);
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
    }
}
