using Sufra.Application.DTOs.Students;

namespace Sufra.Application.Interfaces
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentDto>> GetAllAsync();
        Task<StudentDto?> GetByIdAsync(int id);
        Task<StudentDto> CreateAsync(CreateStudentDto dto);
        
        Task<StudentDto?> UpdateAsync(int id, UpdateStudentDto dto);
        Task<StudentDto?> GetByUniversityIdAsync(string universityId);

        Task<bool> DeleteAsync(int id);
    }
}
