
using System.Text.Json;
using HotelManagementSystem.DTOs.Payments;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Record Internal Payment
// Xử lý nghiệp vụ ghi nhận thanh toán nội bộ.
//
// SWAGGER: https://localhost:7024/swagger
// Method: POST
// Endpoint: /api/invoices/{invoiceId}/payments
// Header: X-Demo-Account-Id
// Body: RecordPaymentRequest
//
// 201: Ghi nhận thanh toán mới.
// 200: Giao dịch đã được ghi nhận trước đó.
// =====================================================

public class PaymentService
{
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F07-05: Record Internal Payment
    //
    // Swagger: https://localhost:7024/swagger
    // POST /api/invoices/{invoiceId}/payments
    //
    // Input:
    // - invoiceId: ID hóa đơn.
    // - accountId: Receptionist thực hiện.
    // - request: Số tiền, phương thức, IdempotencyKey.
    // =================================================
    public async Task<PaymentResultDto> RecordAsync(
        int invoiceId,
        RecordPaymentRequest request,
        int accountId)
    {
        // NhaNT | F07-05
        // Bắt buộc có IdempotencyKey hợp lệ.
        if (request.IdempotencyKey == null ||
            request.IdempotencyKey == Guid.Empty)
        {
            throw new InvoiceBusinessException(
                400, "IdempotencyKey không hợp lệ.");
        }

        // NhaNT | F07-05
        // Số tiền dương và tối đa 2 chữ số thập phân.
        if (request.Amount <= 0 ||
            decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new InvoiceBusinessException(
                400,
                "Amount phải lớn hơn 0 và có tối đa 2 chữ số thập phân.");
        }

        // NhaNT | F07-05
        // Chỉ chấp nhận phương thức trong PaymentMethod.
        if (request.Method == null ||
            !Enum.IsDefined(
                typeof(PaymentMethod),
                request.Method.Value))
        {
            throw new InvoiceBusinessException(
                400, "Phương thức thanh toán không hợp lệ.");
        }

        // NhaNT | F07-05
        // Toàn bộ ghi nhận Payment trong một transaction.
        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F07-05
        // Chỉ Receptionist đang hoạt động được thanh toán.
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null ||
            !account.IsActive ||
            account.Role != AccountRole.Receptionist)
        {
            throw new InvoiceBusinessException(
                403,
                "Tài khoản không có quyền ghi nhận thanh toán.");
        }

        // NhaNT | F07-05
        // Kiểm tra trùng IdempotencyKey toàn hệ thống.
        var existing =
            await _repository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey.Value);

        if (existing != null)
        {
            // NhaNT | F07-05
            // Không tái sử dụng key cho giao dịch khác.
            if (existing.InvoiceId != invoiceId ||
                existing.Amount != request.Amount ||
                existing.Method != request.Method.Value)
            {
                throw new InvoiceBusinessException(
                    409,
                    "IdempotencyKey đã được sử dụng với giao dịch khác.");
            }

            // NhaNT | F07-05
            // Trả kết quả cũ, không tạo Payment mới.
            var sameInvoice =
                await _repository.GetInvoiceAsync(invoiceId);

            if (sameInvoice == null)
            {
                throw new InvoiceBusinessException(
                    404, "Không tìm thấy hóa đơn.");
            }

            var paid =
                await _repository.GetTotalPaidAsync(invoiceId);

            return PaymentMapper.ToResultDto(
                existing,
                sameInvoice.TotalAmount,
                paid,
                "Giao dịch đã được ghi nhận trước đó.");
        }

        // NhaNT | F07-05
        // Kiểm tra Invoice tồn tại.
        var invoice =
            await _repository.GetInvoiceAsync(invoiceId);

        if (invoice == null)
        {
            throw new InvoiceBusinessException(
                404, "Không tìm thấy hóa đơn.");
        }

        // NhaNT | F07-05
        // Chỉ thanh toán cho hóa đơn Finalized.
        if (invoice.Status != InvoiceStatus.Finalized)
        {
            throw new InvoiceBusinessException(
                409, "Hóa đơn chưa Finalized.");
        }

        // NhaNT | F07-05
        // Tính số dư chưa thanh toán.
        var totalPaid =
            await _repository.GetTotalPaidAsync(invoiceId);

        var remaining = invoice.TotalAmount - totalPaid;

        if (remaining <= 0)
        {
            throw new InvoiceBusinessException(
                409, "Hóa đơn đã thanh toán đủ.");
        }

        if (request.Amount > remaining)
        {
            throw new InvoiceBusinessException(
                409,
                $"Số tiền vượt số dư còn lại: {remaining}.");
        }

        // NhaNT | F07-05
        // Tạo Payment với số tiền thực tế nhận.
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
            ReferenceNumber =
                request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _repository.AddPayment(payment);
        await _repository.SaveChangesAsync();

        // NhaNT | F07-05
        // Lưu AuditLog trong cùng transaction.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = accountId,
            ActorUsernameSnapshot = account.Username,
            Action = "RecordPayment",
            EntityType = "Payment",
            EntityId = payment.Id.ToString(),
            EntityCodeSnapshot = payment.ReceiptCode,
            OccurredAt = DateTime.UtcNow,

            Summary =
                $"Ghi nhận thanh toán {payment.Amount} " +
                $"cho Invoice {invoiceId}.",

            NewValuesJson = JsonSerializer.Serialize(new
            {
                payment.InvoiceId,
                payment.Amount,
                Method = payment.Method.ToString(),
                payment.IdempotencyKey
            })
        });

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        // NhaNT | F07-05
        // Mapper trả PaymentResultDto cho Controller.
        return PaymentMapper.ToResultDto(
            payment,
            invoice.TotalAmount,
            totalPaid + payment.Amount,
            "Ghi nhận thanh toán thành công.");
    }
}
