using Sufra.Application.DTOs.Couriers;

namespace Sufra.Application.Interfaces
{
    public interface ICourierService
    {
        // 🟢 جلب جميع المندوبين
        Task<IEnumerable<CourierDto>> GetAllAsync();

        // 🔹 جلب مندوب محدد حسب المعرّف
        Task<CourierDto?> GetByIdAsync(int id);

        // 🟡 جلب المندوبين في منطقة معينة
        Task<IEnumerable<CourierDto>> GetByZoneAsync(int zoneId);

        // 🟩 جلب أول مندوب متاح في منطقة محددة (للتخصيص التلقائي)
        Task<CourierDto?> GetAvailableByZoneAsync(int zoneId);

        // 🟦 إنشاء مندوب جديد
        Task<CourierDto> CreateAsync(CreateCourierDto dto);

        // 🟠 تحديث حالة المندوب (نشط / غير متاح / خارج الخدمة)
        Task<bool> UpdateStatusAsync(int id, string status);
    }
}
