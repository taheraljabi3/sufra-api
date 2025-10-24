using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Batches;
using Sufra.Application.Interfaces;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchesController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchesController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _batchService.GetAllAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBatchDto dto)
        {
            try
            {
                var result = await _batchService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
