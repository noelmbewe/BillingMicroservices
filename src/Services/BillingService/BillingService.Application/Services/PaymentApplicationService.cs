using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using BillingService.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BillingService.Application.Services;

public class PaymentApplicationService : IPaymentService
{
    private readonly ILagoPaymentService _lagoPaymentService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PaymentApplicationService> _logger;

    public PaymentApplicationService(
        ILagoPaymentService lagoPaymentService,
        IMessagePublisher messagePublisher,
        ILogger<PaymentApplicationService> logger)
    {
        _lagoPaymentService = lagoPaymentService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentDto createPaymentDto)
    {
        try
        {
            _logger.LogInformation("Processing payment creation for invoice: {InvoiceId}, amount: {AmountCents}",
                createPaymentDto.Payment.InvoiceId, createPaymentDto.Payment.AmountCents);

            // Convert paid_at from string to DateTime
            var (success, paidAtDateTime) = ConvertPaidAtToDateTime(createPaymentDto.Payment.PaidAtString);

            if (!success)
            {
                _logger.LogError("Invalid paid_at format: {PaidAt}", createPaymentDto.Payment.PaidAtString ?? "null");
                return new CreatePaymentResponseDto
                {
                    Success = false,
                    Message = "Invalid paid_at format. Please use date format like '2025-02-20' or ISO 8601 format",
                    ProcessedAt = DateTime.UtcNow,
                    OriginalPaidAt = createPaymentDto.Payment.PaidAtString
                };
            }

            // Set the converted date
            createPaymentDto.Payment.PaidAt = paidAtDateTime;

            _logger.LogInformation("Converted paid_at from '{OriginalPaidAt}' to DateTime: {ConvertedPaidAt}",
                createPaymentDto.Payment.PaidAtString ?? "current date", paidAtDateTime);

            // Create Lago payment request
            var lagoPaymentRequest = new LagoPaymentRequestDto
            {
                payment = new LagoPaymentDto
                {
                    invoice_id = createPaymentDto.Payment.InvoiceId,
                    amount_cents = createPaymentDto.Payment.AmountCents,
                    reference = createPaymentDto.Payment.Reference,
                    paid_at = paidAtDateTime.ToString("yyyy-MM-dd") // Lago expects date format
                }
            };

            // Send to Lago
            var (lagoSuccess, lagoResponse, errorMessage) = await _lagoPaymentService.CreatePaymentAsync(lagoPaymentRequest);

            if (lagoSuccess && lagoResponse != null)
            {
                // Create domain entity for messaging
                var payment = new Payment
                {
                    Id = lagoResponse.payment.lago_id,
                    InvoiceId = createPaymentDto.Payment.InvoiceId,
                    AmountCents = createPaymentDto.Payment.AmountCents,
                    Currency = createPaymentDto.Payment.Currency,
                    Reference = createPaymentDto.Payment.Reference,
                    PaidAt = paidAtDateTime,
                    Status = lagoResponse.payment.status,
                    CreatedAt = DateTime.UtcNow
                };

                // Publish to RabbitMQ for other services
                await _messagePublisher.PublishAsync(payment, "billing.payment.created");

                _logger.LogInformation("Payment created successfully. Payment ID: {PaymentId}, Invoice ID: {InvoiceId}",
                    lagoResponse.payment.lago_id, createPaymentDto.Payment.InvoiceId);

                return new CreatePaymentResponseDto
                {
                    Success = true,
                    Message = "Payment created successfully",
                    PaymentId = lagoResponse.payment.lago_id,
                    InvoiceId = createPaymentDto.Payment.InvoiceId,
                    AmountCents = createPaymentDto.Payment.AmountCents,
                    Currency = createPaymentDto.Payment.Currency,
                    Reference = createPaymentDto.Payment.Reference,
                    ProcessedAt = DateTime.UtcNow,
                    OriginalPaidAt = createPaymentDto.Payment.PaidAtString,
                    ConvertedPaidAt = paidAtDateTime
                };
            }

            _logger.LogError("Failed to create payment in Lago for invoice: {InvoiceId}. Error: {Error}",
                createPaymentDto.Payment.InvoiceId, errorMessage);

            return new CreatePaymentResponseDto
            {
                Success = false,
                Message = $"Failed to create payment: {errorMessage}",
                InvoiceId = createPaymentDto.Payment.InvoiceId,
                ProcessedAt = DateTime.UtcNow,
                OriginalPaidAt = createPaymentDto.Payment.PaidAtString,
                ConvertedPaidAt = paidAtDateTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment creation for invoice: {InvoiceId}",
                createPaymentDto.Payment.InvoiceId);

            return new CreatePaymentResponseDto
            {
                Success = false,
                Message = $"Error processing payment creation: {ex.Message}",
                InvoiceId = createPaymentDto.Payment.InvoiceId,
                ProcessedAt = DateTime.UtcNow,
                OriginalPaidAt = createPaymentDto.Payment.PaidAtString
            };
        }
    }

    public async Task<RetrievePaymentResponseDto> RetrievePaymentAsync(string paymentId)
    {
        try
        {
            _logger.LogInformation("Retrieving payment: {PaymentId}", paymentId);

            var (success, lagoResponse, errorMessage) = await _lagoPaymentService.RetrievePaymentAsync(paymentId);

            if (success && lagoResponse != null)
            {
                var payment = new PaymentDto
                {
                    Id = lagoResponse.payment.lago_id,
                    InvoiceId = lagoResponse.payment.lago_invoice_id,
                    AmountCents = lagoResponse.payment.amount_cents,
                    Currency = lagoResponse.payment.amount_currency,
                    Reference = lagoResponse.payment.reference,
                    Status = lagoResponse.payment.status,
                    CreatedAt = lagoResponse.payment.created_at,
                    UpdatedAt = lagoResponse.payment.updated_at
                };

                // Parse paid_at from string to DateTime
                if (DateTime.TryParse(lagoResponse.payment.paid_at, out var paidAt))
                {
                    payment.PaidAt = paidAt;
                }

                _logger.LogInformation("Payment retrieved successfully: {PaymentId}", paymentId);

                return new RetrievePaymentResponseDto
                {
                    Success = true,
                    Message = "Payment retrieved successfully",
                    Payment = payment
                };
            }

            _logger.LogWarning("Payment not found or failed to retrieve: {PaymentId}. Error: {Error}",
                paymentId, errorMessage);

            return new RetrievePaymentResponseDto
            {
                Success = false,
                Message = errorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                    ? $"Payment not found: {paymentId}"
                    : $"Failed to retrieve payment: {errorMessage}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment: {PaymentId}", paymentId);

            return new RetrievePaymentResponseDto
            {
                Success = false,
                Message = $"Error retrieving payment: {ex.Message}"
            };
        }
    }

    public async Task<ListPaymentsResponseDto> ListPaymentsAsync(ListPaymentsQueryDto query)
    {
        try
        {
            _logger.LogInformation("Listing payments: Page={Page}, PageSize={PageSize}, InvoiceId={InvoiceId}",
                query.Page, query.PageSize, query.InvoiceId);

            var (success, lagoResponse, errorMessage) = await _lagoPaymentService.ListPaymentsAsync(
                query.Page, query.PageSize, query.InvoiceId);

            if (success && lagoResponse != null)
            {
                var payments = lagoResponse.payments.Select(p => new PaymentDto
                {
                    Id = p.lago_id,
                    InvoiceId = p.lago_invoice_id,
                    AmountCents = p.amount_cents,
                    Currency = p.amount_currency,
                    Reference = p.reference,
                    Status = p.status,
                    CreatedAt = p.created_at,
                    UpdatedAt = p.updated_at,
                    PaidAt = DateTime.TryParse(p.paid_at, out var paidAt) ? paidAt : DateTime.MinValue
                }).ToList();

                // Apply additional client-side filtering if needed
                if (query.FromDate.HasValue)
                {
                    payments = payments.Where(p => p.PaidAt >= query.FromDate.Value).ToList();
                }

                if (query.ToDate.HasValue)
                {
                    payments = payments.Where(p => p.PaidAt <= query.ToDate.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    payments = payments.Where(p => p.Status.Equals(query.Status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                _logger.LogInformation("Retrieved {Count} payments", payments.Count);

                return new ListPaymentsResponseDto
                {
                    Success = true,
                    Message = "Payments retrieved successfully",
                    Payments = payments,
                    TotalCount = lagoResponse.meta.total_count,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            }

            _logger.LogError("Failed to list payments. Error: {Error}", errorMessage);

            return new ListPaymentsResponseDto
            {
                Success = false,
                Message = $"Failed to retrieve payments: {errorMessage}",
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payments");

            return new ListPaymentsResponseDto
            {
                Success = false,
                Message = $"Error listing payments: {ex.Message}",
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    private (bool Success, DateTime PaidAt) ConvertPaidAtToDateTime(string? paidAtString)
    {
        try
        {
            // If empty, use current date
            if (string.IsNullOrWhiteSpace(paidAtString))
            {
                return (true, DateTime.UtcNow.Date); // Use date only, time set to 00:00:00
            }

            DateTime parsedDateTime;

            // Try parsing as various date formats
            string[] formats = {
                "yyyy-MM-dd",                     // 2025-02-20
                "yyyy-MM-ddTHH:mm:ssZ",          // 2025-02-20T14:30:00Z
                "yyyy-MM-ddTHH:mm:ss.fffZ",      // 2025-02-20T14:30:00.123Z
                "yyyy-MM-ddTHH:mm:ss.fffffffZ",  // 2025-02-20T14:30:00.1234567Z
                "yyyy-MM-ddTHH:mm:ss",           // 2025-02-20T14:30:00
                "yyyy-MM-ddTHH:mm:ss.fff",       // 2025-02-20T14:30:00.123
                "yyyy-MM-dd HH:mm:ss",           // 2025-02-20 14:30:00
                "yyyy-MM-dd HH:mm:ssZ"           // 2025-02-20 14:30:00Z
            };

            // First try DateTimeOffset.Parse for full ISO 8601 support
            if (DateTimeOffset.TryParse(paidAtString, out var dateTimeOffset))
            {
                return (true, dateTimeOffset.UtcDateTime);
            }

            // Fall back to DateTime parsing with specific formats
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(paidAtString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                {
                    return (true, parsedDateTime);
                }
            }

            // Last resort: try general DateTime parsing
            if (DateTime.TryParse(paidAtString, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime))
            {
                return (true, parsedDateTime);
            }

            return (false, DateTime.MinValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting paid_at: {PaidAt}", paidAtString);
            return (false, DateTime.MinValue);
        }
    }
}