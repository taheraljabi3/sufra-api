using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Couriers;

namespace Sufra.Application.Interfaces
{
    /// <summary>
    /// 🎯 واجهة خدمة إدارة طلبات الوجبات اليومية
    /// </summary>
    public interface IMealRequestService
    {
        // ============================================================
        // 📋 عمليات الجلب (قراءة)
        // ============================================================
        Task<IEnumerable<MealRequestDto>> GetAllAsync();
        Task<IEnumerable<MealRequestDto>> GetByStudentAsync(int studentId);
        Task<IEnumerable<MealRequestDto>> GetByCourierAsync(int courierId);
        Task<MealRequestDto?> GetByIdAsync(int id);

        // ============================================================
        // ➕ عمليات الإنشاء
        // ============================================================
        Task<MealRequestDto> CreateAsync(CreateMealRequestDto dto);                     // إنشاء من الطالب
        Task<MealRequestDto> CreateAdminAsync(CreateMealRequestFullDto dto);            // إنشاء كامل (للأدمن)
        Task<IEnumerable<MealRequestDto>> BulkCreateAsync(List<CreateMealRequestFullDto> requests); // إدخال دفعة واحدة

        // ============================================================
        // 📢 إشعار وتحديث الطلب
        // ============================================================
        Task<MealRequestDto?> NotifyCouriersOnlyAsync(CreateMealRequestDto dto);        // تحديث + إشعار المندوبين

        // ============================================================
        // 🚴‍♂️ عمليات المندوبين
        // ============================================================
        Task<(bool Success, string Message, int StudentId)> AssignCourierAsync(int requestId, int courierId);
        Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId);

        // ============================================================
        // 🔄 التحديث والحذف
        // ============================================================
        Task<MealRequestDto?> UpdateAsync(MealRequestDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
