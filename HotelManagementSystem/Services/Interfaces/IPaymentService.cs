using HotelManagementSystem.DTOs.Payments;

namespace HotelManagementSystem.Services.Interfaces;

// Developer: NhaNTCE181298
// F07 - Payment Management
public interface IPaymentService
{
    Task<PaymentResultDto> RecordAsync(
        int invoiceId,
        RecordPaymentRequest request,
        int accountId);
}