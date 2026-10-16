
using System.Data;
using System.Text.Json;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Payments;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Record Internal Payment
// OData: Không dùng cho POST.
// =====================================================

public class PaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentResultDto> RecordAsync(
        int invoiceId,
        RecordPaymentRequest request,
        int accountId)
    {
        if (request.IdempotencyKey == null ||
            request.IdempotencyKey == Guid.Empty)
        {
            throw new InvoiceBusinessException(
                400, "IdempotencyKey không hợp lệ.");
        }

        if (request.Amount <= 0 ||
            decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new InvoiceBusinessException(
                400, "Amount phải lớn hơn 0 và có tối đa 2 chữ số thập phân.");
        }

        if (request.Method == null ||
            !Enum.IsDefined(typeof(PaymentMethod), request.Method.Value))
        {
            throw new InvoiceBusinessException(
                400, "Phương thức thanh toán không hợp lệ.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null || !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new InvoiceBusinessException(
                403, "Tài khoản không có quyền ghi nhận thanh toán.");
        }

        // Kiểm tra trùng toàn hệ thống, không chỉ trong Invoice.
        var existing = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.IdempotencyKey == request.IdempotencyKey.Value);

        if (existing != null)
        {
            if (existing.InvoiceId != invoiceId ||
                existing.Amount != request.Amount ||
                existing.Method != request.Method.Value)
            {
                throw new InvoiceBusinessException(
                    409, "IdempotencyKey đã được sử dụng với giao dịch khác.");
            }

            var sameInvoice = await _db.Invoices
                .AsNoTracking()
                .FirstAsync(i => i.Id == invoiceId);

            var paid = await _db.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            return MapResult(
                existing, sameInvoice.TotalAmount, paid,
                "Giao dịch đã được ghi nhận trước đó.");
        }

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            throw new InvoiceBusinessException(
                404, "Không tìm thấy hóa đơn.");

        if (invoice.Status != InvoiceStatus.Finalized)
            throw new InvoiceBusinessException(
                409, "Hóa đơn chưa Finalized.");

        var totalPaid = await _db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var remaining = invoice.TotalAmount - totalPaid;

        if (remaining <= 0)
            throw new InvoiceBusinessException(
                409, "Hóa đơn đã thanh toán đủ.");

        if (request.Amount > remaining)
            throw new InvoiceBusinessException(
                409, $"Số tiền vượt số dư còn lại: {remaining}.");

        // Tạo phiếu thu, chỉ ghi nhận số tiền thực tế nhận.
        var payment = new Payment
        {
            ReceiptCode = "PAY-" +
                Guid.NewGuid().ToString("N")[..20],
            InvoiceId = invoiceId,
            IdempotencyKey = request.IdempotencyKey.Value,
            Amount = request.Amount,
            Method = request.Method.Value,
            PaidAt = DateTime.UtcNow,
            RecordedByAccountId = accountId,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "RecordPayment",
            EntityType = "Payment",
            EntityId = payment.Id.ToString(),
            EntityCodeSnapshot = payment.ReceiptCode,
            OccurredAt = DateTime.UtcNow,
            Summary = $"Ghi nhận thanh toán {payment.Amount} " +
                      $"cho Invoice {invoiceId}.",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                payment.InvoiceId,
                payment.Amount,
                Method = payment.Method.ToString(),
                payment.IdempotencyKey
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapResult(
            payment,
            invoice.TotalAmount,
            totalPaid + payment.Amount,
            "Ghi nhận thanh toán thành công.");
    }

    private static PaymentResultDto MapResult(
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
            RecordedByAccountId = payment.RecordedByAccountId,
            InvoiceTotal = invoiceTotal,
            TotalPaid = totalPaid,
            RemainingAmount = invoiceTotal - totalPaid,
            Message = message
        };
    }
}
