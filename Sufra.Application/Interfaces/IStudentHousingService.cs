using System.Threading.Tasks;
using Sufra.Application.DTOs.StudentHousing;
using Sufra.Domain.Entities;

namespace Sufra.Application.Interfaces
{
    public interface IStudentHousingService
    {
        /// <summary>
        /// إنشاء أو تحديث موقع السكن الحالي للطالب.
        /// </summary>
        Task<bool> UpsertHousingAsync(StudentHousingDto dto);

        /// <summary>
        /// جلب الموقع الحالي للطالب.
        /// </summary>
        Task<StudentHousing?> GetCurrentAsync(int studentId);
    }
}
