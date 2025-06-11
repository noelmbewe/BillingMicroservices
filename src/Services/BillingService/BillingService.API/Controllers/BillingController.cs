// =========================
// Updated API Layer - Controllers
// =========================

// src/Services/BillingService/BillingService.API/Controllers/BillingController.cs
using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BillingService.API.Controllers;

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

    /// <summary>
    /// Creates an event usage record
    /// </summary>
    /// <param name="eventUsage">Event usage data with optional human-readable timestamp</param>
    /// <returns>Processing result</returns>
    /// <remarks>
    /// Sample request (minimal required fields):
    ///
    ///     POST /api/billing/event-usage
    ///     {
    ///         "event": {
    ///             "externalSubscriptionId": "trobadoh_265masm",
    ///             "code": "masm_claims265"
    ///         }
    ///     }
    ///
    /// Sample request (with optional fields):
    ///
    ///     POST /api/billing/event-usage
    ///     {
    ///         "event": {
    ///             "transactionId": "unique-transaction-123",
    ///             "externalSubscriptionId": "trobadoh_265masm",
    ///             "code": "masm_claims265",
    ///             "timestamp": "2025-06-11T11:49:02Z",
    ///             "properties": {
    ///                 "endpoint": "/api/claims",
    ///                 "method": "GET"
    ///             }
    ///         }
    ///     }
    ///
    /// Optional timestamp formats (leave empty for current server time):
    /// - ISO 8601 with Z: "2025-06-11T11:49:02Z"
    /// - ISO 8601 with milliseconds: "2025-06-11T11:49:02.123Z"
    /// - ISO 8601 without Z: "2025-06-11T11:49:02"
    /// - Date only: "2025-06-11" (time defaults to 00:00:00)
    /// - Space separated: "2025-06-11 11:49:02"
    /// 
    /// Optional fields:
    /// - transactionId: Auto-generated if not provided
    /// - timestamp: Current server time if not provided
    /// - properties: Can be omitted entirely
    /// </remarks>
    [HttpPost("event-usage")]
    public async Task<ActionResult<EventUsageResponseDto>> CreateEventUsage([FromBody] EventUsageDto eventUsage)
    {
        try
        {
            _logger.LogInformation("Received event usage request for subscription: {ExternalSubscriptionId}, timestamp: {Timestamp}",
                eventUsage.Event.ExternalSubscriptionId, eventUsage.Event.TimestampString ?? "current time");

            // Validate required fields
            var validationErrors = ValidateEventUsage(eventUsage);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new EventUsageResponseDto
                {
                    Success = false,
                    Message = $"Validation failed: {string.Join(", ", validationErrors)}",
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

    /// <summary>
    /// Get current server time in various formats for reference
    /// </summary>
    /// <returns>Current time in different formats</returns>
    [HttpGet("current-time")]
    public IActionResult GetCurrentTime()
    {
        var now = DateTime.UtcNow;
        var dateTimeOffset = new DateTimeOffset(now);

        return Ok(new
        {
            CurrentTimeUtc = now,
            Formats = new
            {
                ISO8601_Z = now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ISO8601_WithMilliseconds = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ISO8601_NoZ = now.ToString("yyyy-MM-ddTHH:mm:ss"),
                DateOnly = now.ToString("yyyy-MM-dd"),
                SpaceSeparated = now.ToString("yyyy-MM-dd HH:mm:ss"),
                UnixTimestamp = dateTimeOffset.ToUnixTimeSeconds()
            },
            Note = "Use any of the above formats (except UnixTimestamp) in your timestamp field"
        });
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

    private List<string> ValidateEventUsage(EventUsageDto eventUsage)
    {
        var errors = new List<string>();

        if (eventUsage?.Event == null)
        {
            errors.Add("Event data is required");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(eventUsage.Event.ExternalSubscriptionId))
        {
            errors.Add("ExternalSubscriptionId is required");
        }

        if (string.IsNullOrWhiteSpace(eventUsage.Event.Code))
        {
            errors.Add("Code is required");
        }

        // Note: TimestampString can be empty (will default to current time)
        // but if provided, it will be validated in the service layer

        return errors;
    }
}