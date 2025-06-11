using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using BillingService.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BillingService.Application.Services;

public class BillingApplicationService : IBillingService
{
    private readonly ILagoService _lagoService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<BillingApplicationService> _logger;

    public BillingApplicationService(
        ILagoService lagoService,
        IMessagePublisher messagePublisher,
        ILogger<BillingApplicationService> logger)
    {
        _lagoService = lagoService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<EventUsageResponseDto> ProcessEventUsageAsync(EventUsageDto eventUsageDto)
    {
        try
        {
            _logger.LogInformation("Processing event usage for subscription: {ExternalSubscriptionId}",
                eventUsageDto.Event.ExternalSubscriptionId);

            // Convert timestamp from string to Unix timestamp
            var (success, unixTimestamp, parsedDateTime) = ConvertTimestampToUnix(eventUsageDto.Event.TimestampString);

            if (!success)
            {
                _logger.LogError("Invalid timestamp format: {Timestamp}", eventUsageDto.Event.TimestampString ?? "null");
                return new EventUsageResponseDto
                {
                    Success = false,
                    Message = "Invalid timestamp format. Please use ISO 8601 format (e.g., '2025-06-11T11:49:02Z') or leave empty for current time",
                    TransactionId = eventUsageDto.Event.TransactionId ?? Guid.NewGuid().ToString(),
                    ProcessedAt = DateTime.UtcNow,
                    OriginalTimestamp = eventUsageDto.Event.TimestampString
                };
            }

            // Set the converted timestamp
            eventUsageDto.Event.Timestamp = unixTimestamp;

            // Generate transaction ID if not provided
            if (string.IsNullOrEmpty(eventUsageDto.Event.TransactionId))
            {
                eventUsageDto.Event.TransactionId = $"tx_{Guid.NewGuid():N}";
            }

            _logger.LogInformation("Converted timestamp from '{OriginalTimestamp}' to Unix timestamp: {UnixTimestamp}",
                eventUsageDto.Event.TimestampString ?? "current time", unixTimestamp);

            // Create domain entity
            var eventUsage = new EventUsage
            {
                ExternalSubscriptionId = eventUsageDto.Event.ExternalSubscriptionId,
                EventName = eventUsageDto.Event.Code,
                Timestamp = parsedDateTime,
                Properties = eventUsageDto.Event.Properties ?? new Dictionary<string, object>(),
                TransactionId = eventUsageDto.Event.TransactionId
            };

            // Convert to Lago format
            var lagoEvent = new LagoEventDto
            {
                transaction_id = eventUsageDto.Event.TransactionId,
                external_subscription_id = eventUsageDto.Event.ExternalSubscriptionId,
                code = eventUsageDto.Event.Code,
                timestamp = unixTimestamp,
                properties = eventUsageDto.Event.Properties ?? new Dictionary<string, object>()
            };

            // Send to Lago
            var lagoSuccess = await _lagoService.SendEventToLagoAsync(lagoEvent);

            if (lagoSuccess)
            {
                // Publish to RabbitMQ for other services
                await _messagePublisher.PublishAsync(eventUsage, "billing.event.processed");

                _logger.LogInformation("Event usage processed successfully. Transaction ID: {TransactionId}",
                    eventUsage.TransactionId);

                return new EventUsageResponseDto
                {
                    Success = true,
                    Message = "Event usage processed successfully",
                    TransactionId = eventUsage.TransactionId,
                    ProcessedAt = DateTime.UtcNow,
                    OriginalTimestamp = eventUsageDto.Event.TimestampString,
                    ConvertedTimestamp = unixTimestamp
                };
            }

            _logger.LogError("Failed to send event to Lago for transaction: {TransactionId}",
                eventUsage.TransactionId);

            return new EventUsageResponseDto
            {
                Success = false,
                Message = "Failed to process event usage",
                TransactionId = eventUsage.TransactionId,
                ProcessedAt = DateTime.UtcNow,
                OriginalTimestamp = eventUsageDto.Event.TimestampString,
                ConvertedTimestamp = unixTimestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event usage for subscription: {ExternalSubscriptionId}",
                eventUsageDto.Event.ExternalSubscriptionId);

            return new EventUsageResponseDto
            {
                Success = false,
                Message = $"Error processing event usage: {ex.Message}",
                TransactionId = eventUsageDto.Event.TransactionId ?? Guid.NewGuid().ToString(),
                ProcessedAt = DateTime.UtcNow,
                OriginalTimestamp = eventUsageDto.Event.TimestampString
            };
        }
    }

    private (bool Success, long UnixTimestamp, DateTime ParsedDateTime) ConvertTimestampToUnix(string timestampString)
    {
        try
        {
            // If empty, use current time
            if (string.IsNullOrWhiteSpace(timestampString))
            {
                var now = DateTime.UtcNow;
                return (true, ((DateTimeOffset)now).ToUnixTimeSeconds(), now);
            }

            DateTime parsedDateTime;

            // Try parsing as ISO 8601 formats
            string[] formats = {
                "yyyy-MM-ddTHH:mm:ssZ",           // 2025-06-11T11:49:02Z
                "yyyy-MM-ddTHH:mm:ss.fffZ",       // 2025-06-11T11:49:02.123Z
                "yyyy-MM-ddTHH:mm:ss.fffffffZ",   // 2025-06-11T11:49:02.1234567Z
                "yyyy-MM-ddTHH:mm:ss",            // 2025-06-11T11:49:02
                "yyyy-MM-ddTHH:mm:ss.fff",        // 2025-06-11T11:49:02.123
                "yyyy-MM-dd HH:mm:ss",            // 2025-06-11 11:49:02
                "yyyy-MM-dd HH:mm:ssZ",           // 2025-06-11 11:49:02Z
                "yyyy-MM-dd"                      // 2025-06-11 (will use 00:00:00)
            };

            // First try DateTimeOffset.Parse for full ISO 8601 support
            if (DateTimeOffset.TryParse(timestampString, out var dateTimeOffset))
            {
                return (true, dateTimeOffset.ToUnixTimeSeconds(), dateTimeOffset.UtcDateTime);
            }

            // Fall back to DateTime parsing with specific formats
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(timestampString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                {
                    var unixTimestamp = ((DateTimeOffset)parsedDateTime).ToUnixTimeSeconds();
                    return (true, unixTimestamp, parsedDateTime);
                }
            }

            // Last resort: try general DateTime parsing
            if (DateTime.TryParse(timestampString, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDateTime))
            {
                var unixTimestamp = ((DateTimeOffset)parsedDateTime).ToUnixTimeSeconds();
                return (true, unixTimestamp, parsedDateTime);
            }

            return (false, 0, DateTime.MinValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting timestamp: {Timestamp}", timestampString);
            return (false, 0, DateTime.MinValue);
        }
    }
}