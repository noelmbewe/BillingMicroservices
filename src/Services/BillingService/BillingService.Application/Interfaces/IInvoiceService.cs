namespace BillingService.Application.Interfaces;

using BillingService.Application.DTOs;
using global::BillingService.Application.DTOs;

public interface IInvoiceService
{
    Task<InvoiceListDto?> GetInvoicesAsync(InvoiceQueryDto query);
    Task<InvoiceDto?> GetInvoiceByIdAsync(string invoiceId);
    Task<InvoiceDownloadDto?> DownloadInvoiceAsync(string invoiceId);
    Task<byte[]?> GetInvoicePdfAsync(string invoiceId);
}
