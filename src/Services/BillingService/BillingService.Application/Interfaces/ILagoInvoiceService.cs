namespace BillingService.Application.Interfaces;

using BillingService.Application.DTOs;
using global::BillingService.Application.DTOs;

public interface ILagoInvoiceService
{
    Task<InvoiceListDto?> GetInvoicesFromLagoAsync(InvoiceQueryDto query);
    Task<InvoiceDto?> GetInvoiceFromLagoAsync(string invoiceId);
    Task<byte[]?> DownloadInvoicePdfFromLagoAsync(string invoiceId);
    Task<InvoiceDto?> FinalizeInvoiceAsync(string invoiceId);
    Task<InvoiceDto?> RefreshInvoiceAsync(string invoiceId);
}