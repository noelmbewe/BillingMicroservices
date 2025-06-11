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
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {lagoApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _logger.LogInformation("LagoService initialized with BaseUrl: {BaseUrl}", lagoBaseUrl);
    }

    public async Task<bool> SendEventToLagoAsync(LagoEventDto lagoEvent)
    {
        try
        {
            _logger.LogInformation("Sending event to Lago: TransactionId={TransactionId}, SubscriptionId={SubscriptionId}, Code={Code}, Timestamp={Timestamp}",
                lagoEvent.transaction_id, lagoEvent.external_subscription_id, lagoEvent.code, lagoEvent.timestamp);

            // Validate required fields
            if (string.IsNullOrEmpty(lagoEvent.transaction_id))
            {
                _logger.LogError("transaction_id is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(lagoEvent.external_subscription_id))
            {
                _logger.LogError("external_subscription_id is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(lagoEvent.code))
            {
                _logger.LogError("code is null or empty");
                return false;
            }

            // Create the payload exactly as Lago expects it
            var payload = new
            {
                @event = new
                {
                    transaction_id = lagoEvent.transaction_id,
                    external_subscription_id = lagoEvent.external_subscription_id,
                    code = lagoEvent.code,
                    timestamp = lagoEvent.timestamp,
                    properties = lagoEvent.properties ?? new Dictionary<string, object>()
                }
            };

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var json = JsonConvert.SerializeObject(payload, jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Lago request payload: {Payload}", json);

            // Log the full request details
            _logger.LogInformation("Making POST request to: {Url}", "/api/v1/events");
            _logger.LogInformation("Request headers: Authorization=Bearer {ApiKey}",
                _configuration["Lago:ApiKey"]?.Substring(0, Math.Min(8, _configuration["Lago:ApiKey"]?.Length ?? 0)) + "...");

            var response = await _httpClient.PostAsync("/api/v1/events", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Lago response: StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                response.StatusCode, response.IsSuccessStatusCode);
            _logger.LogInformation("Lago response content: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Successfully sent event to Lago: {TransactionId}", lagoEvent.transaction_id);
                return true;
            }

            _logger.LogError("❌ Failed to send event to Lago. Status: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseContent}",
                response.StatusCode, response.ReasonPhrase, responseContent);

            // Log specific error details
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogError("Bad Request - Check if subscription exists and billable metric code is correct");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized - Check API key configuration");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                _logger.LogError("Unprocessable Entity - Check data format and required fields");
            }

            return false;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error occurred while sending event to Lago: {TransactionId}. Message: {Message}",
                lagoEvent.transaction_id, httpEx.Message);
            return false;
        }
        catch (TaskCanceledException tcEx)
        {
            _logger.LogError(tcEx, "Request timeout occurred while sending event to Lago: {TransactionId}",
                lagoEvent.transaction_id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending event to Lago: {TransactionId}. Exception: {ExceptionType}",
                lagoEvent.transaction_id, ex.GetType().Name);
            return false;
        }
    }
}