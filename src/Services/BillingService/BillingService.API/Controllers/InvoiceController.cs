using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BillingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILagoInvoiceService _lagoInvoiceService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        ILagoInvoiceService lagoInvoiceService,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _lagoInvoiceService = lagoInvoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of invoices with optional filtering
    /// </summary>
    /// <param name="externalCustomerId">Filter by customer external ID</param>
    /// <param name="externalSubscriptionId">Filter by subscription external ID</param>
    /// <param name="status">Filter by invoice status (draft, finalized, voided)</param>
    /// <param name="paymentStatus">Filter by payment status (pending, succeeded, failed)</param>
    /// <param name="issuingDateFrom">Filter invoices issued from this date (YYYY-MM-DD)</param>
    /// <param name="issuingDateTo">Filter invoices issued to this date (YYYY-MM-DD)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="perPage">Items per page (default: 20, max: 100)</param>
    /// <returns>List of invoices</returns>
    [HttpGet]
    public async Task<ActionResult<InvoiceListDto>> GetInvoices(
        [FromQuery] string? externalCustomerId = null,
        [FromQuery] string? externalSubscriptionId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] string? issuingDateFrom = null,
        [FromQuery] string? issuingDateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        try
        {
            _logger.LogInformation("Getting invoices list - Page: {Page}, PerPage: {PerPage}, Status: {Status}",
                page, perPage, status ?? "All");

            var query = new InvoiceQueryDto
            {
                ExternalCustomerId = externalCustomerId,
                ExternalSubscriptionId = externalSubscriptionId,
                Status = status,
                PaymentStatus = paymentStatus,
                Page = Math.Max(1, page),
                PerPage = Math.Min(Math.Max(1, perPage), 100)
            };

            // Parse date filters if provided
            if (!string.IsNullOrEmpty(issuingDateFrom) && DateTime.TryParse(issuingDateFrom, out var dateFrom))
                query.IssuingDateFrom = dateFrom;

            if (!string.IsNullOrEmpty(issuingDateTo) && DateTime.TryParse(issuingDateTo, out var dateTo))
                query.IssuingDateTo = dateTo;

            var result = await _invoiceService.GetInvoicesAsync(query);

            if (result == null)
            {
                return StatusCode(500, new { Message = "Failed to retrieve invoices" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get specific invoice by ID
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>Invoice details</returns>
    [HttpGet("{invoiceId}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Getting invoice by ID: {InvoiceId}", invoiceId);

            var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (result == null)
            {
                return NotFound(new { Message = $"Invoice with ID {invoiceId} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Download invoice as PDF file
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>PDF file download</returns>
    [HttpGet("{invoiceId}/download")]
    public async Task<IActionResult> DownloadInvoice([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Downloading invoice: {InvoiceId}", invoiceId);

            var downloadDto = await _invoiceService.DownloadInvoiceAsync(invoiceId);

            if (downloadDto == null)
            {
                return NotFound(new { Message = $"Invoice with ID {invoiceId} not found or PDF not available" });
            }

            if (downloadDto.PdfContent == null || downloadDto.PdfContent.Length == 0)
            {
                return StatusCode(500, new { Message = "PDF content is not available" });
            }

            return File(
                downloadDto.PdfContent,
                downloadDto.ContentType,
                downloadDto.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while downloading invoice" });
        }
    }

    /// <summary>
    /// Get invoice PDF content as byte array (for API consumers)
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>PDF content as base64 encoded string</returns>
    [HttpGet("{invoiceId}/pdf")]
    public async Task<IActionResult> GetInvoicePdf([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Getting PDF content for invoice: {InvoiceId}", invoiceId);

            var pdfContent = await _invoiceService.GetInvoicePdfAsync(invoiceId);

            if (pdfContent == null || pdfContent.Length == 0)
            {
                return NotFound(new { Message = $"PDF not found for invoice with ID {invoiceId}" });
            }

            // Return as JSON with base64 encoded content
            var response = new
            {
                InvoiceId = invoiceId,
                ContentType = "application/pdf",
                Content = Convert.ToBase64String(pdfContent),
                Size = pdfContent.Length,
                Generated = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF content for invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while retrieving PDF content" });
        }
    }

    /// <summary>
    /// Get invoice PDF content directly (returns PDF bytes)
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>PDF file content</returns>
    [HttpGet("{invoiceId}/pdf/raw")]
    public async Task<IActionResult> GetInvoicePdfRaw([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Getting raw PDF content for invoice: {InvoiceId}", invoiceId);

            var pdfContent = await _invoiceService.GetInvoicePdfAsync(invoiceId);

            if (pdfContent == null || pdfContent.Length == 0)
            {
                return NotFound(new { Message = $"PDF not found for invoice with ID {invoiceId}" });
            }

            return File(pdfContent, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting raw PDF content for invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while retrieving PDF content" });
        }
    }

    /// <summary>
    /// Finalize a draft invoice
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>Updated invoice details</returns>
    [HttpPut("{invoiceId}/finalize")]
    public async Task<ActionResult<InvoiceDto>> FinalizeInvoice([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Finalizing invoice: {InvoiceId}", invoiceId);

            var result = await _lagoInvoiceService.FinalizeInvoiceAsync(invoiceId);

            if (result == null)
            {
                return NotFound(new { Message = $"Invoice with ID {invoiceId} not found or cannot be finalized" });
            }

            _logger.LogInformation("Successfully finalized invoice: {InvoiceId}", invoiceId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while finalizing invoice" });
        }
    }

    /// <summary>
    /// Refresh invoice data from Lago
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>Updated invoice details</returns>
    [HttpPut("{invoiceId}/refresh")]
    public async Task<ActionResult<InvoiceDto>> RefreshInvoice([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Refreshing invoice: {InvoiceId}", invoiceId);

            var result = await _lagoInvoiceService.RefreshInvoiceAsync(invoiceId);

            if (result == null)
            {
                return NotFound(new { Message = $"Invoice with ID {invoiceId} not found or cannot be refreshed" });
            }

            _logger.LogInformation("Successfully refreshed invoice: {InvoiceId}", invoiceId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing invoice: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while refreshing invoice" });
        }
    }

    /// <summary>
    /// Get invoice metadata and status
    /// </summary>
    /// <param name="invoiceId">Invoice ID (Lago ID)</param>
    /// <returns>Invoice metadata</returns>
    [HttpGet("{invoiceId}/status")]
    public async Task<IActionResult> GetInvoiceStatus([Required] string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(new { Message = "Invoice ID is required" });
            }

            _logger.LogInformation("Getting invoice status: {InvoiceId}", invoiceId);

            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice with ID {invoiceId} not found" });
            }

            var statusResponse = new
            {
                InvoiceId = invoiceId,
                Number = invoice.Invoice.Number,
                Status = invoice.Invoice.Status,
                PaymentStatus = invoice.Invoice.PaymentStatus,
                InvoiceType = invoice.Invoice.InvoiceType,
                IssuingDate = invoice.Invoice.IssuingDate,
                PaymentDueDate = invoice.Invoice.PaymentDueDate,
                TotalAmountCents = invoice.Invoice.TotalAmountCents,
                Currency = invoice.Invoice.Currency,
                Customer = new
                {
                    Name = invoice.Invoice.Customer.Name,
                    Email = invoice.Invoice.Customer.Email,
                    ExternalId = invoice.Invoice.Customer.ExternalId
                },
                PdfAvailable = !string.IsNullOrEmpty(invoice.Invoice.FileUrl)
            };

            return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice status: {InvoiceId}", invoiceId);
            return StatusCode(500, new { Message = "Internal server error while getting invoice status" });
        }
    }
}