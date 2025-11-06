using Sufra.Application.DTOs.Subscriptions;

namespace Sufra.Application.Interfaces
{
    public interface ISubscriptionService
    {
        // 📋 جلب جميع الاشتراكات
        Task<IEnumerable<SubscriptionDto>> GetAllAsync();

        // 🔍 جلب اشتراك محدد بالمعرّف
        Task<SubscriptionDto?> GetByIdAsync(int id);

        // 🟢 جلب الاشتراك النشط لطالب معين
        Task<SubscriptionDto?> GetActiveByStudentAsync(int studentId);

        // ➕ إنشاء اشتراك جديد
        Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto);

        // ✏️ تحديث اشتراك
        Task<SubscriptionDto?> UpdateAsync(int id, UpdateSubscriptionDto dto);

        // ❌ إلغاء اشتراك
        Task<bool> CancelAsync(int id);
    }
}
