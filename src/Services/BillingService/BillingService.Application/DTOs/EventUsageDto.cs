using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BillingService.Application.DTOs;

public class EventUsageDto
{
    public EventData Event { get; set; } = new EventData();
}

public class EventData
{
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    public string ExternalSubscriptionId { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional timestamp in ISO 8601 format (e.g., "2025-06-11T11:49:02Z" or "2025-06-11T11:49:02.123Z")
    /// If not provided or empty, current server time will be used automatically
    /// Will be automatically converted to Unix timestamp for Lago
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? TimestampString { get; set; }

    /// <summary>
    /// Internal property for Unix timestamp conversion - not exposed in API
    /// </summary>
    [JsonIgnore]
    public long Timestamp { get; set; }

    /// <summary>
    /// Optional properties dictionary for additional event metadata
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}

public class EventUsageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string? OriginalTimestamp { get; set; }
    public long ConvertedTimestamp { get; set; }
}