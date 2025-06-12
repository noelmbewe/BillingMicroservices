using System.Text.Json.Serialization;

namespace BillingService.Application.DTOs;

public class InvoiceDto
{
    [JsonPropertyName("invoice")]
    public InvoiceData Invoice { get; set; } = new();
}

public class InvoiceData
{
    [JsonPropertyName("lago_id")]
    public string LagoId { get; set; } = string.Empty;

    [JsonPropertyName("sequential_id")]
    public int SequentialId { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("issuing_date")]
    public string IssuingDate { get; set; } = string.Empty;

    [JsonPropertyName("payment_due_date")]
    public string PaymentDueDate { get; set; } = string.Empty;

    [JsonPropertyName("net_payment_term")]
    public int NetPaymentTerm { get; set; }

    [JsonPropertyName("invoice_type")]
    public string InvoiceType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("fees_amount_cents")]
    public long FeesAmountCents { get; set; }

    [JsonPropertyName("coupons_amount_cents")]
    public long CouponsAmountCents { get; set; }

    [JsonPropertyName("taxes_amount_cents")]
    public long TaxesAmountCents { get; set; }

    [JsonPropertyName("credit_notes_amount_cents")]
    public long CreditNotesAmountCents { get; set; }

    [JsonPropertyName("sub_total_excluding_taxes_amount_cents")]
    public long SubTotalExcludingTaxesAmountCents { get; set; }

    [JsonPropertyName("sub_total_including_taxes_amount_cents")]
    public long SubTotalIncludingTaxesAmountCents { get; set; }

    [JsonPropertyName("total_amount_cents")]
    public long TotalAmountCents { get; set; }

    [JsonPropertyName("prepaid_credit_amount_cents")]
    public long PrepaidCreditAmountCents { get; set; }

    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("customer")]
    public InvoiceCustomer Customer { get; set; } = new();

    [JsonPropertyName("subscriptions")]
    public List<InvoiceSubscription> Subscriptions { get; set; } = new();

    [JsonPropertyName("fees")]
    public List<InvoiceFee> Fees { get; set; } = new();
}

public class InvoiceCustomer
{
    [JsonPropertyName("lago_id")]
    public string LagoId { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class InvoiceSubscription
{
    [JsonPropertyName("lago_id")]
    public string LagoId { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("lago_customer_id")]
    public string LagoCustomerId { get; set; } = string.Empty;

    [JsonPropertyName("plan_code")]
    public string PlanCode { get; set; } = string.Empty;
}

public class InvoiceFee
{
    [JsonPropertyName("lago_id")]
    public string LagoId { get; set; } = string.Empty;

    [JsonPropertyName("lago_group_id")]
    public string? LagoGroupId { get; set; }

    [JsonPropertyName("item")]
    public InvoiceFeeItem Item { get; set; } = new();

    [JsonPropertyName("amount_cents")]
    public long AmountCents { get; set; }

    [JsonPropertyName("amount_currency")]
    public string AmountCurrency { get; set; } = string.Empty;

    [JsonPropertyName("taxes_amount_cents")]
    public long TaxesAmountCents { get; set; }

    [JsonPropertyName("units")]
    public decimal Units { get; set; }

    [JsonPropertyName("events_count")]
    public int EventsCount { get; set; }
}

public class InvoiceFeeItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("invoice_display_name")]
    public string InvoiceDisplayName { get; set; } = string.Empty;
}

public class InvoiceListDto
{
    [JsonPropertyName("invoices")]
    public List<InvoiceData> Invoices { get; set; } = new();

    [JsonPropertyName("meta")]
    public InvoiceMetadata Meta { get; set; } = new();
}

public class InvoiceMetadata
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("next_page")]
    public int? NextPage { get; set; }

    [JsonPropertyName("prev_page")]
    public int? PrevPage { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

public class InvoiceQueryDto
{
    public string? ExternalCustomerId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? Status { get; set; } // draft, finalized, voided
    public string? PaymentStatus { get; set; } // pending, succeeded, failed
    public DateTime? IssuingDateFrom { get; set; }
    public DateTime? IssuingDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 20;
}

public class InvoiceDownloadDto
{
    public string InvoiceId { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/pdf";
    public string FileName { get; set; } = string.Empty;
}