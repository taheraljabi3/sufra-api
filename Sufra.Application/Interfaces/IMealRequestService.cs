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
        // 🔹 عمليات القراءة
        // ============================================================

        /// <summary>
        /// جلب جميع الطلبات في النظام (للمشرفين)
        /// وتشمل معلومات الطالب والمنطقة والاشتراك وتاريخ الوجبة وحالة الدفع.
        /// </summary>
        Task<IEnumerable<MealRequestDto>> GetAllAsync();

        /// <summary>
        /// جلب الطلبات الخاصة بطالب معين (تشمل حالة الدفع وتاريخ الوجبة).
        /// </summary>
        Task<IEnumerable<MealRequestDto>> GetByStudentAsync(int studentId);

        /// <summary>
        /// جلب الطلبات المتاحة أو المسندة لمندوب معيّن (حسب منطق منطقته).
        /// </summary>
        Task<IEnumerable<MealRequestDto>> GetByCourierAsync(int courierId);

        /// <summary>
        /// جلب تفاصيل طلب واحد حسب رقم المعرّف.
        /// </summary>
        Task<MealRequestDto?> GetByIdAsync(int id);

        // ============================================================
        // 🔹 عمليات الإنشاء والتحديث
        // ============================================================

        /// <summary>
        /// إنشاء طلب وجبة جديد.
        /// - يمنع التكرار لنفس اليوم والفترة.
        /// - إذا كان "توصيل"، يتم إشعار جميع المندوبين في نفس المنطقة.
        /// </summary>
Task<MealRequestDto> CreateAsync(CreateMealRequestDto dto);

        /// <summary>
        /// تحديث حالة الطلب أو الملاحظات أو الدفع مع إشعار الطالب بالحالة الجديدة.
        /// </summary>
        Task<MealRequestDto?> UpdateAsync(MealRequestDto dto);

        // ============================================================
        // 🚴‍♂️ عمليات المندوب
        // ============================================================

        /// <summary>
        /// قبول الطلب من أحد المندوبين.
        /// - يتم إسناد الطلب لأول مندوب يقبله فقط.
        /// - يتم إشعار الطالب باسم المندوب.
        /// - يتم إشعار المندوب نفسه بتأكيد الإسناد.
        /// </summary>
        Task<(bool Success, string Message, int StudentId)> AssignCourierAsync(int requestId, int courierId);

        // ============================================================
        // 🔹 عمليات الحذف
        // ============================================================

        /// <summary>
        /// حذف طلب وجبة (للإدارة فقط).
        /// </summary>
        Task<bool> DeleteAsync(int id);

        // ============================================================
        // 🧭 جلب المندوبين في منطقة محددة
        // ============================================================

        /// <summary>
        /// جلب جميع المندوبين النشطين في منطقة معينة.
        /// </summary>
        Task<IEnumerable<CourierDto>> GetCouriersByZoneAsync(int zoneId);

        // ============================================================
        // 📢 إنشاء سجل فعلي + إرسال إشعارات لجميع المندوبين في المنطقة
        // ============================================================
        /// <summary>
        /// يقوم بإنشاء سجل فعلي في قاعدة البيانات وإرسال إشعارات للمندوبين
        /// بوجود طلب جديد في منطقتهم (قبل القبول).
        /// </summary>
        Task<MealRequestDto?> NotifyCouriersOnlyAsync(CreateMealRequestDto dto);

        Task<MealRequestDto> CreateAsync(MealRequestDto dto);

    }
}
