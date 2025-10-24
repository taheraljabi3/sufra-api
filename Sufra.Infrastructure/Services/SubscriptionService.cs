using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.Interfaces;
using Sufra.Domain.Entities;
using Sufra.Infrastructure.Persistence;

namespace Sufra.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly SufraDbContext _context;
        private readonly IMapper _mapper;

        public SubscriptionService(SufraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SubscriptionDto>> GetAllAsync()
        {
            var subs = await _context.Subscriptions
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SubscriptionDto>>(subs);
        }

        public async Task<SubscriptionDto?> GetByIdAsync(int id)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            return sub == null ? null : _mapper.Map<SubscriptionDto>(sub);
        }

        public async Task<SubscriptionDto?> GetActiveByStudentAsync(int studentId)
        {
            var sub = await _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == "active")
                .FirstOrDefaultAsync();

            return sub == null ? null : _mapper.Map<SubscriptionDto>(sub);
        }

        public async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto)
        {
            // التحقق من وجود اشتراك نشط
            var existing = await _context.Subscriptions
                .AnyAsync(s => s.StudentId == dto.StudentId && s.Status == "active");

            if (existing)
                throw new InvalidOperationException("الطالب لديه اشتراك نشط بالفعل.");

            var sub = _mapper.Map<Subscription>(dto);
            sub.StartDate = DateTime.UtcNow;
            sub.EndDate = sub.StartDate.AddMonths(1);
            sub.Status = "active";
            sub.CreatedAt = DateTime.UtcNow;

            _context.Subscriptions.Add(sub);
            await _context.SaveChangesAsync();

            return _mapper.Map<SubscriptionDto>(sub);
        }

        public async Task<bool> CancelAsync(int id)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null) return false;

            sub.Status = "cancelled";
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
