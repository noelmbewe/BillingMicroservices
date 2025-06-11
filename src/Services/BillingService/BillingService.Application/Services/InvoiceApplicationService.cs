using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingService.Application.Services;

public class InvoiceApplicationService : IInvoiceService
{
    private readonly ILagoInvoiceService _lagoInvoiceService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<InvoiceApplicationService> _logger;

    public InvoiceApplicationService(
        ILagoInvoiceService lagoInvoiceService,
        IMessagePublisher messagePublisher,
        ILogger<InvoiceApplicationService> logger)
    {
        _lagoInvoiceService = lagoInvoiceService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<InvoiceListDto?> GetInvoicesAsync(InvoiceQueryDto query)
    {
        try
        {
            _logger.LogInformation("Fetching invoices with query: Page={Page}, PerPage={PerPage}, Status={Status}",
                query.Page, query.PerPage, query.Status ?? "All");

            var invoices = await _lagoInvoiceService.GetInvoicesFromLagoAsync(query);

            if (invoices != null)
            {
                _logger.LogInformation("Successfully retrieved {Count} invoices", invoices.Invoices.Count);

                // Publish event for analytics/auditing
                await _messagePublisher.PublishAsync(new
                {
                    EventType = "InvoicesRetrieved",
                    Query = query,
                    ResultCount = invoices.Invoices.Count,
                    Timestamp = DateTime.UtcNow
                }, "billing.invoices.retrieved");
            }

            return invoices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoices with query: {@Query}", query);
            throw;
        }
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(string invoiceId)
    {
        try
        {
            _logger.LogInformation("Fetching invoice by ID: {InvoiceId}", invoiceId);

            var invoice = await _lagoInvoiceService.GetInvoiceFromLagoAsync(invoiceId);

            if (invoice != null)
            {
                _logger.LogInformation("Successfully retrieved invoice: {InvoiceNumber}", invoice.Invoice.Number);

                // Publish event for analytics/auditing
                await _messagePublisher.PublishAsync(new
                {
                    EventType = "InvoiceRetrieved",
                    InvoiceId = invoiceId,
                    InvoiceNumber = invoice.Invoice.Number,
                    CustomerId = invoice.Invoice.Customer.ExternalId,
                    Timestamp = DateTime.UtcNow
                }, "billing.invoice.retrieved");
            }

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoice by ID: {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<InvoiceDownloadDto?> DownloadInvoiceAsync(string invoiceId)
    {
        try
        {
            _logger.LogInformation("Downloading invoice: {InvoiceId}", invoiceId);

            // First get invoice details
            var invoice = await _lagoInvoiceService.GetInvoiceFromLagoAsync(invoiceId);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found: {InvoiceId}", invoiceId);
                return null;
            }

            // Download PDF content
            var pdfContent = await _lagoInvoiceService.DownloadInvoicePdfFromLagoAsync(invoiceId);
            if (pdfContent == null || pdfContent.Length == 0)
            {
                _logger.LogWarning("PDF content not available for invoice: {InvoiceId}", invoiceId);
                return null;
            }

            var downloadDto = new InvoiceDownloadDto
            {
                InvoiceId = invoiceId,
                Number = invoice.Invoice.Number,
                CustomerName = invoice.Invoice.Customer.Name,
                PdfContent = pdfContent,
                ContentType = "application/pdf",
                FileName = $"Invoice_{invoice.Invoice.Number}_{DateTime.UtcNow:yyyyMMdd}.pdf"
            };

            _logger.LogInformation("Successfully downloaded invoice PDF: {InvoiceNumber}, Size: {Size} bytes",
                invoice.Invoice.Number, pdfContent.Length);

            // Publish event for analytics/auditing
            await _messagePublisher.PublishAsync(new
            {
                EventType = "InvoiceDownloaded",
                InvoiceId = invoiceId,
                InvoiceNumber = invoice.Invoice.Number,
                CustomerId = invoice.Invoice.Customer.ExternalId,
                CustomerName = invoice.Invoice.Customer.Name,
                FileSizeBytes = pdfContent.Length,
                Timestamp = DateTime.UtcNow
            }, "billing.invoice.downloaded");

            return downloadDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice: {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<byte[]?> GetInvoicePdfAsync(string invoiceId)
    {
        try
        {
            _logger.LogInformation("Getting PDF content for invoice: {InvoiceId}", invoiceId);

            var pdfContent = await _lagoInvoiceService.DownloadInvoicePdfFromLagoAsync(invoiceId);

            if (pdfContent != null && pdfContent.Length > 0)
            {
                _logger.LogInformation("Successfully retrieved PDF content for invoice: {InvoiceId}, Size: {Size} bytes",
                    invoiceId, pdfContent.Length);
            }
            else
            {
                _logger.LogWarning("No PDF content available for invoice: {InvoiceId}", invoiceId);
            }

            return pdfContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF content for invoice: {InvoiceId}", invoiceId);
            throw;
        }
    }
}