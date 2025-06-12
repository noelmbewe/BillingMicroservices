using BillingService.Application.DTOs;

namespace BillingService.Application.Interfaces;

public interface IPaymentService
{
    Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
    Task<RetrievePaymentResponseDto> RetrievePaymentAsync(string paymentId);
    Task<ListPaymentsResponseDto> ListPaymentsAsync(ListPaymentsQueryDto query);
}

public interface ILagoPaymentService
{
    Task<(bool Success, LagoPaymentResponseDto? Response, string? ErrorMessage)> CreatePaymentAsync(LagoPaymentRequestDto paymentRequest);
    Task<(bool Success, LagoPaymentResponseDto? Response, string? ErrorMessage)> RetrievePaymentAsync(string paymentId);
    Task<(bool Success, LagoPaymentsListResponseDto? Response, string? ErrorMessage)> ListPaymentsAsync(int page = 1, int pageSize = 20, string? invoiceId = null);
}