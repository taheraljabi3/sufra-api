using Sufra.Application.DTOs.Batches;

namespace Sufra.Application.Interfaces
{
    public interface IBatchService
    {
        Task<IEnumerable<BatchDto>> GetAllAsync();
        Task<BatchDto> CreateAsync(CreateBatchDto dto);
    }
}
