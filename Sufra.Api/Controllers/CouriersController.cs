using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Couriers;
using Sufra.Application.Interfaces;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouriersController : ControllerBase
    {
        private readonly ICourierService _courierService;

        public CouriersController(ICourierService courierService)
        {
            _courierService = courierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _courierService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _courierService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "المندوب غير موجود" });
            return Ok(result);
        }

        [HttpGet("zone/{zoneId}")]
        public async Task<IActionResult> GetByZone(int zoneId)
        {
            var result = await _courierService.GetByZoneAsync(zoneId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCourierDto dto)
        {
            try
            {
                var result = await _courierService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            var success = await _courierService.UpdateStatusAsync(id, status);
            if (!success)
                return NotFound(new { message = "المندوب غير موجود" });

            return Ok(new { message = $"تم تحديث حالة المندوب إلى {status}" });
        }
    }
}
