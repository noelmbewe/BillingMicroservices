namespace BillingService.Application.DTOs;

public class LagoEventDto
{
    public string transaction_id { get; set; } = string.Empty;
    public string external_subscription_id { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public long timestamp { get; set; }
    public Dictionary<string, object> properties { get; set; } = new();
}