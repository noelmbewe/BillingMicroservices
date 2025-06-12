using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BillingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

   
    [HttpPost]
    public async Task<ActionResult<CreatePaymentResponseDto>> CreatePayment([FromBody] CreatePaymentDto createPayment)
    {
        try
        {
            _logger.LogInformation("Received payment creation request for invoice: {InvoiceId}, amount: {AmountCents} cents",
                createPayment.Payment.InvoiceId, createPayment.Payment.AmountCents);

            // Validate required fields
            var validationErrors = ValidateCreatePayment(createPayment);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new CreatePaymentResponseDto
                {
                    Success = false,
                    Message = $"Validation failed: {string.Join(", ", validationErrors)}",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            var result = await _paymentService.CreatePaymentAsync(createPayment);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment creation request");

            return StatusCode(500, new CreatePaymentResponseDto
            {
                Success = false,
                Message = "Internal server error",
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieves a specific payment by ID
    /// </summary>
    /// <param name="paymentId">The payment ID to retrieve</param>
    /// <returns>Payment details</returns>
    /// <remarks>
    /// Sample request:
    ///     GET /api/payment/486b147a-02a1-4ccf-8603-f3541fc25e7a
    /// </remarks>
    [HttpGet("{paymentId}")]
    public async Task<ActionResult<RetrievePaymentResponseDto>> RetrievePayment([FromRoute] string paymentId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return BadRequest(new RetrievePaymentResponseDto
                {
                    Success = false,
                    Message = "Payment ID is required"
                });
            }

            _logger.LogInformation("Retrieving payment: {PaymentId}", paymentId);

            var result = await _paymentService.RetrievePaymentAsync(paymentId);

            if (result.Success)
            {
                return Ok(result);
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment: {PaymentId}", paymentId);

            return StatusCode(500, new RetrievePaymentResponseDto
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Lists all payments with optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="invoiceId">Filter by invoice ID</param>
    /// <param name="status">Filter by payment status</param>
    /// <param name="fromDate">Filter payments from this date (ISO 8601)</param>
    /// <param name="toDate">Filter payments to this date (ISO 8601)</param>
    /// <returns>List of payments</returns>
    /// <remarks>
    /// Sample requests:
    ///     GET /api/payment
    ///     GET /api/payment?page=2&amp;pageSize=10
    ///     GET /api/payment?invoiceId=486b147a-02a1-4ccf-8603-f3541fc25e7a
    ///     GET /api/payment?status=succeeded&amp;fromDate=2025-01-01&amp;toDate=2025-12-31
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<ListPaymentsResponseDto>> ListPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? invoiceId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            _logger.LogInformation("Listing payments: Page={Page}, PageSize={PageSize}, InvoiceId={InvoiceId}, Status={Status}",
                page, pageSize, invoiceId, status);

            var query = new ListPaymentsQueryDto
            {
                Page = page,
                PageSize = pageSize,
                InvoiceId = invoiceId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _paymentService.ListPaymentsAsync(query);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payments");

            return StatusCode(500, new ListPaymentsResponseDto
            {
                Success = false,
                Message = "Internal server error"
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
                DateOnly = now.ToString("yyyy-MM-dd"),
                ISO8601_Z = now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ISO8601_WithMilliseconds = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ISO8601_NoZ = now.ToString("yyyy-MM-ddTHH:mm:ss"),
                SpaceSeparated = now.ToString("yyyy-MM-dd HH:mm:ss"),
                UnixTimestamp = dateTimeOffset.ToUnixTimeSeconds()
            },
            Note = "Use DateOnly format for paid_at field, or any ISO 8601 format"
        });
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow, Service = "Payment" });
    }

    private List<string> ValidateCreatePayment(CreatePaymentDto createPayment)
    {
        var errors = new List<string>();

        if (createPayment?.Payment == null)
        {
            errors.Add("Payment data is required");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(createPayment.Payment.InvoiceId))
        {
            errors.Add("InvoiceId is required");
        }

        if (createPayment.Payment.AmountCents <= 0)
        {
            errors.Add("AmountCents must be greater than 0");
        }

        // Validate currency if provided (should be 3-letter code)
        if (!string.IsNullOrWhiteSpace(createPayment.Payment.Currency) &&
            createPayment.Payment.Currency.Length != 3)
        {
            errors.Add("Currency must be a 3-letter currency code (e.g., MWK, USD)");
        }

        return errors;
    }
}