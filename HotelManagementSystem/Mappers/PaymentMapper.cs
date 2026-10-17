
using HotelManagementSystem.DTOs.Payments;
using HotelManagementSystem.Entities;

namespace HotelManagementSystem.Mappers;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Payment Mapper
// Chuyển Payment Entity thành PaymentResultDto.
// =====================================================

public static class PaymentMapper
{
    // NhaNT | F07-05
    // Dùng cho giao dịch mới và giao dịch ghi nhận trước đó.
    public static PaymentResultDto ToResultDto(
        Payment payment,
        decimal invoiceTotal,
        decimal totalPaid,
        string message)
    {
        return new PaymentResultDto
        {
            Id = payment.Id,
            ReceiptCode = payment.ReceiptCode,
            InvoiceId = payment.InvoiceId,
            IdempotencyKey = payment.IdempotencyKey,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            PaidAt = payment.PaidAt,
            RecordedByAccountId =
                payment.RecordedByAccountId,

            InvoiceTotal = invoiceTotal,
            TotalPaid = totalPaid,
            RemainingAmount = invoiceTotal - totalPaid,
            Message = message
        };
    }
}
