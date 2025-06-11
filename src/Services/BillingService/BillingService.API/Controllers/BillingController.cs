namespace BillingService.API.Controllers;

using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IBillingService billingService, ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    [HttpPost("event-usage")]
    public async Task<ActionResult<EventUsageResponseDto>> CreateEventUsage([FromBody] EventUsageDto eventUsage)
    {
        try
        {
            _logger.LogInformation("Received event usage request for subscription: {ExternalSubscriptionId}",
                eventUsage.Event.ExternalSubscriptionId);

            if (string.IsNullOrEmpty(eventUsage.Event.ExternalSubscriptionId))
            {
                return BadRequest(new EventUsageResponseDto
                {
                    Success = false,
                    Message = "ExternalSubscriptionId is required",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            var result = await _billingService.ProcessEventUsageAsync(eventUsage);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event usage request");

            return StatusCode(500, new EventUsageResponseDto
            {
                Success = false,
                Message = "Internal server error",
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}