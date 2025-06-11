namespace BillingService.Application.DTOs;

public class EventUsageDto
{
    public EventData Event { get; set; } = new EventData();
}

public class EventData
{
    public string TransactionId { get; set; } = string.Empty;
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class EventUsageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}