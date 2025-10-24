using Sufra.Application.DTOs.Subscriptions;

namespace Sufra.Application.Interfaces
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionDto>> GetAllAsync();
        Task<SubscriptionDto?> GetByIdAsync(int id);
        Task<SubscriptionDto?> GetActiveByStudentAsync(int studentId);
        Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto);
        Task<bool> CancelAsync(int id);
    }
}
