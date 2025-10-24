using Microsoft.AspNetCore.Mvc;
using Sufra.Application.DTOs.Subscriptions;
using Sufra.Application.Interfaces;

namespace Sufra.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _subscriptionService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _subscriptionService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "الاشتراك غير موجود" });
            return Ok(result);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetActiveByStudent(int studentId)
        {
            var result = await _subscriptionService.GetActiveByStudentAsync(studentId);
            if (result == null)
                return NotFound(new { message = "لا يوجد اشتراك نشط لهذا الطالب" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSubscriptionDto dto)
        {
            try
            {
                var result = await _subscriptionService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var success = await _subscriptionService.CancelAsync(id);
            if (!success)
                return NotFound(new { message = "الاشتراك غير موجود" });
            return Ok(new { message = "تم إلغاء الاشتراك بنجاح" });
        }
    }
}
