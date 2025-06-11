namespace BillingService.Domain.Entities;

public class EventUsage
{
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
}