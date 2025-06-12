using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BillingService.Application.DTOs;

// Create Payment DTOs
public class CreatePaymentDto
{
    public PaymentData Payment { get; set; } = new PaymentData();
}

public class PaymentData
{
    [Required]
    public string InvoiceId { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int AmountCents { get; set; }

    public string? Reference { get; set; }

    /// <summary>
    /// Payment date in format "2025-02-20" or ISO 8601 format
    /// If not provided, current date will be used
    /// </summary>
    [JsonPropertyName("paid_at")]
    public string? PaidAtString { get; set; }

    /// <summary>
    /// Currency code - defaults to MWK (Malawian Kwacha)
    /// </summary>
    public string Currency { get; set; } = "MWK";

    /// <summary>
    /// Internal property for date conversion - not exposed in API
    /// </summary>
    [JsonIgnore]
    public DateTime PaidAt { get; set; }
}

// Response DTOs
public class CreatePaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
    public string? InvoiceId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "MWK";
    public string? Reference { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? OriginalPaidAt { get; set; }
    public DateTime ConvertedPaidAt { get; set; }
}

// Retrieve Payment DTOs
public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "MWK";
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RetrievePaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaymentDto? Payment { get; set; }
}

// List Payments DTOs
public class ListPaymentsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ListPaymentsQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? InvoiceId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// Lago-specific DTOs (for external API calls)
public class LagoPaymentDto
{
    public string invoice_id { get; set; } = string.Empty;
    public int amount_cents { get; set; }
    public string? reference { get; set; }
    public string paid_at { get; set; } = string.Empty;
}

public class LagoPaymentRequestDto
{
    public LagoPaymentDto payment { get; set; } = new LagoPaymentDto();
}

public class LagoPaymentResponseDto
{
    public LagoPaymentDetailsDto payment { get; set; } = new LagoPaymentDetailsDto();
}

public class LagoPaymentDetailsDto
{
    public string lago_id { get; set; } = string.Empty;
    public string lago_invoice_id { get; set; } = string.Empty;
    public int amount_cents { get; set; }
    public string amount_currency { get; set; } = string.Empty;
    public string? reference { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public string paid_at { get; set; } = string.Empty;
}

public class LagoPaymentsListResponseDto
{
    public List<LagoPaymentDetailsDto> payments { get; set; } = new List<LagoPaymentDetailsDto>();
    public LagoMetaDto meta { get; set; } = new LagoMetaDto();
}

public class LagoMetaDto
{
    public int current_page { get; set; }
    public int next_page { get; set; }
    public int prev_page { get; set; }
    public int total_pages { get; set; }
    public int total_count { get; set; }
}