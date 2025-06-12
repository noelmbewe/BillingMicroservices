namespace BillingService.Domain.Entities;

public class Payment
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "MWK";
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    // Helper properties
    public decimal AmountInMajorUnit => AmountCents / 100.0m;
    public bool IsSuccessful => Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase);
}