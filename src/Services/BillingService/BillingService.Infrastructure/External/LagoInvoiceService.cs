using BillingService.Application.DTOs;
using BillingService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace BillingService.Infrastructure.External;

public class LagoInvoiceService : ILagoInvoiceService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LagoInvoiceService> _logger;

    public LagoInvoiceService(HttpClient httpClient, IConfiguration configuration, ILogger<LagoInvoiceService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        var lagoBaseUrl = _configuration["Lago:BaseUrl"] ?? "http://localhost:3000";
        var lagoApiKey = _configuration["Lago:ApiKey"] ?? "";

        _httpClient.BaseAddress = new Uri(lagoBaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {lagoApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Increase timeout for PDF downloads
    }

    public async Task<InvoiceListDto?> GetInvoicesFromLagoAsync(InvoiceQueryDto query)
    {
        try
        {
            var queryParams = BuildQueryParameters(query);
            var url = $"/api/v1/invoices?{queryParams}";

            _logger.LogInformation("Fetching invoices from Lago: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Lago invoices response: Status={StatusCode}, Content length={ContentLength}",
                response.StatusCode, content.Length);

            if (response.IsSuccessStatusCode)
            {
                var invoices = JsonConvert.DeserializeObject<InvoiceListDto>(content);
                _logger.LogInformation("Successfully retrieved {Count} invoices from Lago",
                    invoices?.Invoices.Count ?? 0);
                return invoices;
            }

            _logger.LogError("Failed to fetch invoices from Lago. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, content);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching invoices from Lago");
            return null;
        }
    }

    public async Task<InvoiceDto?> GetInvoiceFromLagoAsync(string invoiceId)
    {
        try
        {
            var url = $"/api/v1/invoices/{invoiceId}";

            _logger.LogInformation("Fetching invoice from Lago: {InvoiceId}", invoiceId);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Lago invoice response: Status={StatusCode}, Content length={ContentLength}",
                response.StatusCode, content.Length);

            if (response.IsSuccessStatusCode)
            {
                var invoice = JsonConvert.DeserializeObject<InvoiceDto>(content);
                _logger.LogInformation("Successfully retrieved invoice from Lago: {InvoiceNumber}",
                    invoice?.Invoice.Number ?? "Unknown");
                return invoice;
            }

            _logger.LogError("Failed to fetch invoice from Lago. InvoiceId: {InvoiceId}, Status: {StatusCode}, Response: {Response}",
                invoiceId, response.StatusCode, content);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching invoice from Lago: {InvoiceId}", invoiceId);
            return null;
        }
    }

    public async Task<byte[]?> DownloadInvoicePdfFromLagoAsync(string invoiceId)
    {
        try
        {
            var url = $"/api/v1/invoices/{invoiceId}/download";

            _logger.LogInformation("Downloading invoice PDF from Lago: {InvoiceId}", invoiceId);

            // Create a new request with PDF headers
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/pdf");

            var response = await _httpClient.SendAsync(request);

            _logger.LogDebug("Lago PDF download response: Status={StatusCode}, ContentType={ContentType}, ContentLength={ContentLength}",
                response.StatusCode, response.Content.Headers.ContentType?.MediaType, response.Content.Headers.ContentLength);

            if (response.IsSuccessStatusCode)
            {
                var pdfContent = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Successfully downloaded PDF from Lago: {InvoiceId}, Size: {Size} bytes",
                    invoiceId, pdfContent.Length);
                return pdfContent;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to download PDF from Lago. InvoiceId: {InvoiceId}, Status: {StatusCode}, Response: {Response}",
                invoiceId, response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading PDF from Lago: {InvoiceId}", invoiceId);
            return null;
        }
    }

    public async Task<InvoiceDto?> FinalizeInvoiceAsync(string invoiceId)
    {
        try
        {
            var url = $"/api/v1/invoices/{invoiceId}/finalize";

            _logger.LogInformation("Finalizing invoice in Lago: {InvoiceId}", invoiceId);

            var response = await _httpClient.PutAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var invoice = JsonConvert.DeserializeObject<InvoiceDto>(content);
                _logger.LogInformation("Successfully finalized invoice in Lago: {InvoiceId}", invoiceId);
                return invoice;
            }

            _logger.LogError("Failed to finalize invoice in Lago. InvoiceId: {InvoiceId}, Status: {StatusCode}, Response: {Response}",
                invoiceId, response.StatusCode, content);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while finalizing invoice in Lago: {InvoiceId}", invoiceId);
            return null;
        }
    }

    public async Task<InvoiceDto?> RefreshInvoiceAsync(string invoiceId)
    {
        try
        {
            var url = $"/api/v1/invoices/{invoiceId}/refresh";

            _logger.LogInformation("Refreshing invoice in Lago: {InvoiceId}", invoiceId);

            var response = await _httpClient.PutAsync(url, new StringContent("{}", Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var invoice = JsonConvert.DeserializeObject<InvoiceDto>(content);
                _logger.LogInformation("Successfully refreshed invoice in Lago: {InvoiceId}", invoiceId);
                return invoice;
            }

            _logger.LogError("Failed to refresh invoice in Lago. InvoiceId: {InvoiceId}, Status: {StatusCode}, Response: {Response}",
                invoiceId, response.StatusCode, content);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while refreshing invoice in Lago: {InvoiceId}", invoiceId);
            return null;
        }
    }

    private string BuildQueryParameters(InvoiceQueryDto query)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.ExternalCustomerId))
            parameters.Add($"external_customer_id={HttpUtility.UrlEncode(query.ExternalCustomerId)}");

        if (!string.IsNullOrWhiteSpace(query.ExternalSubscriptionId))
            parameters.Add($"external_subscription_id={HttpUtility.UrlEncode(query.ExternalSubscriptionId)}");

        if (!string.IsNullOrWhiteSpace(query.Status))
            parameters.Add($"status={HttpUtility.UrlEncode(query.Status)}");

        if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
            parameters.Add($"payment_status={HttpUtility.UrlEncode(query.PaymentStatus)}");

        if (query.IssuingDateFrom.HasValue)
            parameters.Add($"issuing_date_from={query.IssuingDateFrom.Value:yyyy-MM-dd}");

        if (query.IssuingDateTo.HasValue)
            parameters.Add($"issuing_date_to={query.IssuingDateTo.Value:yyyy-MM-dd}");

        parameters.Add($"page={query.Page}");
        parameters.Add($"per_page={Math.Min(query.PerPage, 100)}"); // Limit to 100 per page

        return string.Join("&", parameters);
    }
}