namespace BillingService.Application.Interfaces;

using BillingService.Application.DTOs;

public interface ILagoService
{
    Task<bool> SendEventToLagoAsync(LagoEventDto lagoEvent);
}