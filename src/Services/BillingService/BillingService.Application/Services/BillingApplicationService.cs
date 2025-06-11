namespace BillingService.Application.Services;

using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using BillingService.Domain.Entities;
using Microsoft.Extensions.Logging;

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

            // Create domain entity
            var eventUsage = new EventUsage
            {
                ExternalSubscriptionId = eventUsageDto.Event.ExternalSubscriptionId,
                EventName = eventUsageDto.Event.Code,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(eventUsageDto.Event.Timestamp).UtcDateTime,
                Properties = eventUsageDto.Event.Properties,
                TransactionId = eventUsageDto.Event.TransactionId
            };

            // Convert to Lago format
            var lagoEvent = new LagoEventDto
            {
                transaction_id = eventUsageDto.Event.TransactionId,
                external_subscription_id = eventUsageDto.Event.ExternalSubscriptionId,
                code = eventUsageDto.Event.Code,
                timestamp = eventUsageDto.Event.Timestamp,
                properties = eventUsageDto.Event.Properties
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
                    ProcessedAt = DateTime.UtcNow
                };
            }

            _logger.LogError("Failed to send event to Lago for transaction: {TransactionId}",
                eventUsage.TransactionId);

            return new EventUsageResponseDto
            {
                Success = false,
                Message = "Failed to process event usage",
                TransactionId = eventUsage.TransactionId,
                ProcessedAt = DateTime.UtcNow
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
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}