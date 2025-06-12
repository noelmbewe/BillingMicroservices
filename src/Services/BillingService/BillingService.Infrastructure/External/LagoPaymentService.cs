using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace BillingService.Infrastructure.External;

public class LagoPaymentService : ILagoPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LagoPaymentService> _logger;

    public LagoPaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<LagoPaymentService> logger)
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

        _logger.LogInformation("LagoPaymentService initialized with BaseUrl: {BaseUrl}", lagoBaseUrl);
    }

    public async Task<(bool Success, LagoPaymentResponseDto? Response, string? ErrorMessage)> CreatePaymentAsync(LagoPaymentRequestDto paymentRequest)
    {
        try
        {
            _logger.LogInformation("Creating payment in Lago: InvoiceId={InvoiceId}, AmountCents={AmountCents}, PaidAt={PaidAt}",
                paymentRequest.payment.invoice_id, paymentRequest.payment.amount_cents, paymentRequest.payment.paid_at);

            // Validate required fields
            if (string.IsNullOrEmpty(paymentRequest.payment.invoice_id))
            {
                var error = "invoice_id is required";
                _logger.LogError(error);
                return (false, null, error);
            }

            if (paymentRequest.payment.amount_cents <= 0)
            {
                var error = "amount_cents must be greater than 0";
                _logger.LogError(error);
                return (false, null, error);
            }

            if (string.IsNullOrEmpty(paymentRequest.payment.paid_at))
            {
                var error = "paid_at is required";
                _logger.LogError(error);
                return (false, null, error);
            }

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var json = JsonConvert.SerializeObject(paymentRequest, jsonSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Lago payment request payload: {Payload}", json);

            var response = await _httpClient.PostAsync("/api/v1/payments", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Lago payment response: StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                response.StatusCode, response.IsSuccessStatusCode);
            _logger.LogInformation("Lago payment response content: {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                var lagoResponse = JsonConvert.DeserializeObject<LagoPaymentResponseDto>(responseContent);
                _logger.LogInformation("✅ Successfully created payment in Lago: {PaymentId}", lagoResponse?.payment.lago_id);
                return (true, lagoResponse, null);
            }

            var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            _logger.LogError("❌ Failed to create payment in Lago. Status: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseContent}",
                response.StatusCode, response.ReasonPhrase, responseContent);

            // Log specific error details
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                errorMessage = "Bad Request - Check if invoice exists and amount is valid";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                errorMessage = "Unauthorized - Check API key configuration";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                errorMessage = "Unprocessable Entity - Check data format and required fields";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = "Not Found - Invoice may not exist";
            }

            return (false, null, errorMessage);
        }
        catch (HttpRequestException httpEx)
        {
            var error = $"HTTP error occurred while creating payment: {httpEx.Message}";
            _logger.LogError(httpEx, error);
            return (false, null, error);
        }
        catch (TaskCanceledException tcEx)
        {
            var error = "Request timeout occurred while creating payment";
            _logger.LogError(tcEx, error);
            return (false, null, error);
        }
        catch (JsonException jsonEx)
        {
            var error = $"JSON parsing error: {jsonEx.Message}";
            _logger.LogError(jsonEx, error);
            return (false, null, error);
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error occurred while creating payment: {ex.Message}";
            _logger.LogError(ex, error);
            return (false, null, error);
        }
    }

    public async Task<(bool Success, LagoPaymentResponseDto? Response, string? ErrorMessage)> RetrievePaymentAsync(string paymentId)
    {
        try
        {
            _logger.LogInformation("Retrieving payment from Lago: PaymentId={PaymentId}", paymentId);

            if (string.IsNullOrWhiteSpace(paymentId))
            {
                var error = "Payment ID is required";
                _logger.LogError(error);
                return (false, null, error);
            }

            var response = await _httpClient.GetAsync($"/api/v1/payments/{paymentId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Lago retrieve payment response: StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                response.StatusCode, response.IsSuccessStatusCode);

            if (response.IsSuccessStatusCode)
            {
                var lagoResponse = JsonConvert.DeserializeObject<LagoPaymentResponseDto>(responseContent);
                _logger.LogInformation("✅ Successfully retrieved payment from Lago: {PaymentId}", paymentId);
                return (true, lagoResponse, null);
            }

            var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            _logger.LogError("❌ Failed to retrieve payment from Lago. PaymentId: {PaymentId}, Status: {StatusCode}, Response: {ResponseContent}",
                paymentId, response.StatusCode, responseContent);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = $"Payment not found: {paymentId}";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                errorMessage = "Unauthorized - Check API key configuration";
            }

            return (false, null, errorMessage);
        }
        catch (HttpRequestException httpEx)
        {
            var error = $"HTTP error occurred while retrieving payment: {httpEx.Message}";
            _logger.LogError(httpEx, error);
            return (false, null, error);
        }
        catch (TaskCanceledException tcEx)
        {
            var error = "Request timeout occurred while retrieving payment";
            _logger.LogError(tcEx, error);
            return (false, null, error);
        }
        catch (JsonException jsonEx)
        {
            var error = $"JSON parsing error: {jsonEx.Message}";
            _logger.LogError(jsonEx, error);
            return (false, null, error);
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error occurred while retrieving payment: {ex.Message}";
            _logger.LogError(ex, error);
            return (false, null, error);
        }
    }

    public async Task<(bool Success, LagoPaymentsListResponseDto? Response, string? ErrorMessage)> ListPaymentsAsync(int page = 1, int pageSize = 20, string? invoiceId = null)
    {
        try
        {
            _logger.LogInformation("Listing payments from Lago: Page={Page}, PageSize={PageSize}, InvoiceId={InvoiceId}",
                page, pageSize, invoiceId);

            // Build query parameters
            var queryParams = new List<string>
            {
                $"page={page}",
                $"per_page={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(invoiceId))
            {
                queryParams.Add($"invoice_id={Uri.EscapeDataString(invoiceId)}");
            }

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/api/v1/payments?{queryString}";

            var response = await _httpClient.GetAsync(endpoint);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Lago list payments response: StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                response.StatusCode, response.IsSuccessStatusCode);

            if (response.IsSuccessStatusCode)
            {
                var lagoResponse = JsonConvert.DeserializeObject<LagoPaymentsListResponseDto>(responseContent);
                _logger.LogInformation("✅ Successfully retrieved {Count} payments from Lago", lagoResponse?.payments.Count ?? 0);
                return (true, lagoResponse, null);
            }

            var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            _logger.LogError("❌ Failed to list payments from Lago. Status: {StatusCode}, Response: {ResponseContent}",
                response.StatusCode, responseContent);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                errorMessage = "Unauthorized - Check API key configuration";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                errorMessage = "Bad Request - Check query parameters";
            }

            return (false, null, errorMessage);
        }
        catch (HttpRequestException httpEx)
        {
            var error = $"HTTP error occurred while listing payments: {httpEx.Message}";
            _logger.LogError(httpEx, error);
            return (false, null, error);
        }
        catch (TaskCanceledException tcEx)
        {
            var error = "Request timeout occurred while listing payments";
            _logger.LogError(tcEx, error);
            return (false, null, error);
        }
        catch (JsonException jsonEx)
        {
            var error = $"JSON parsing error: {jsonEx.Message}";
            _logger.LogError(jsonEx, error);
            return (false, null, error);
        }
        catch (Exception ex)
        {
            var error = $"Unexpected error occurred while listing payments: {ex.Message}";
            _logger.LogError(ex, error);
            return (false, null, error);
        }
    }
}