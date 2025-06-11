namespace BillingService.Application.Interfaces;

using BillingService.Application.DTOs;

public interface IBillingService
{
    Task<EventUsageResponseDto> ProcessEventUsageAsync(EventUsageDto eventUsage);
}