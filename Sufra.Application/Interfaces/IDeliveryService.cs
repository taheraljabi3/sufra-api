using Sufra.Application.DTOs.Deliveries;
using Sufra.Application.DTOs.MealRequests;
using Sufra.Application.DTOs.Couriers;


namespace Sufra.Application.Interfaces
{
    public interface IDeliveryService
    {
        // 🟦 جلب جميع مهام المندوب
        Task<IEnumerable<DeliveryProofDto>> GetByCourierAsync(int courierId);

        // 🟩 تأكيد تسليم الطلب
        Task<DeliveryProofDto> ConfirmDeliveryAsync(CreateDeliveryProofDto dto);

        // 🟨 تعيين الطلب الجديد تلقائيًا إلى مندوب في نفس المنطقة
        Task AssignToCourierAsync(MealRequestDto mealRequest);

        Task<IEnumerable<DeliveryProofDto>> GetAllAsync();

                /// <summary>
        /// جلب المندوبين المرتبطين بمنطقة معينة (Zone).
        /// تُستخدم عند إنشاء الطلب لإشعار المندوبين في نفس المنطقة.
        /// </summary>
        Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId);


    }
}
