using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace BillingService.Infrastructure.External;

public class LagoService : ILagoService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LagoService> _logger;

    public LagoService(HttpClient httpClient, IConfiguration configuration, ILogger<LagoService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var lagoBaseUrl = _configuration["Lago:BaseUrl"] ?? "http://localhost:3000";
        var lagoApiKey = _configuration["Lago:ApiKey"] ?? "";

        _httpClient.BaseAddress = new Uri(lagoBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {lagoApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<bool> SendEventToLagoAsync(LagoEventDto lagoEvent)
    {
        try
        {
            _logger.LogInformation("Sending event to Lago: {TransactionId}, Subscription: {SubscriptionId}, Code: {Code}",
                lagoEvent.transaction_id, lagoEvent.external_subscription_id, lagoEvent.code);

            var payload = new { @event = lagoEvent };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Lago request payload: {Payload}", json);

            var response = await _httpClient.PostAsync("/api/v1/events", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Lago response: Status={StatusCode}, Content={ResponseContent}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent event to Lago: {TransactionId}", lagoEvent.transaction_id);
                return true;
            }

            _logger.LogError("Failed to send event to Lago. Status: {StatusCode}, Response: {ResponseContent}",
                response.StatusCode, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending event to Lago: {TransactionId}",
                lagoEvent.transaction_id);
            return false;
        }
    }
}