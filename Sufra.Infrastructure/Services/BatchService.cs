using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Batches;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;

namespace Sufra.Infrastructure.Services
{
    public class BatchService : IBatchService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public BatchService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BatchDto>> GetAllAsync()
        {
            var batches = await _context.Batches
                .Include(b => b.Items)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<BatchDto>>(batches);
        }

        public async Task<BatchDto> CreateAsync(CreateBatchDto dto)
        {
            // 🔹 اجلب الطلبات غير المجمّعة (queued) لنفس المنطقة والفترة
            var requests = await _context.MealRequests
                .Where(r => r.ZoneId == dto.ZoneId && r.Period == dto.Period && r.Status == "queued")
                .ToListAsync();

            if (!requests.Any())
                throw new InvalidOperationException("لا توجد طلبات جاهزة للتجميع لهذه المنطقة والفترة.");

            // 🔹 أنشئ دفعة جديدة
            var batch = _mapper.Map<Batch>(dto);
            batch.CreatedAt = DateTime.UtcNow;
            batch.Status = "pending";
            batch.PickupTime = DateTime.UtcNow.AddMinutes(15); // مثال مبدئي بعد 15 دقيقة

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync(); // نحتاج الـ Id للدفعة

            // 🔹 أنشئ BatchItems واربط الطلبات
            foreach (var req in requests)
            {
                _context.BatchItems.Add(new BatchItem
                {
                    BatchId = batch.Id,
                    ReqId = req.Id
                });

                req.Status = "batched";
            }

            await _context.SaveChangesAsync();

            // 🔹 إعادة الدفعة النهائية كـ DTO
            return _mapper.Map<BatchDto>(batch);
        }
    }
}
