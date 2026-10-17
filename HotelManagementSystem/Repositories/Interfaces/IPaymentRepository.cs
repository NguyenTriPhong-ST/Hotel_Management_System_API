
using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Interfaces;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Record Internal Payment
// Interface truy cập dữ liệu thanh toán.
//
// Swagger: https://localhost:7024/swagger
// POST /api/invoices/{invoiceId}/payments
// OData: Không áp dụng cho POST.
// =====================================================

public interface IPaymentRepository
{
    // NhaNT | F07-05 - Transaction Serializable.
    Task<IDbContextTransaction> BeginTransactionAsync();

    // NhaNT | F07-05 - Kiểm tra tài khoản.
    Task<Account?> GetAccountAsync(int accountId);

    // NhaNT | F07-05 - Kiểm tra giao dịch trùng.
    Task<Payment?> GetByIdempotencyKeyAsync(Guid key);

    // NhaNT | F07-05 - Lấy hóa đơn.
    Task<Invoice?> GetInvoiceAsync(int invoiceId);

    // NhaNT | F07-05 - Tổng số tiền đã trả.
    Task<decimal> GetTotalPaidAsync(int invoiceId);

    // NhaNT | F07-05 - Tạo phiếu thanh toán.
    void AddPayment(Payment payment);

    // NhaNT | F07-05 - Lưu lịch sử thao tác.
    void AddAuditLog(AuditLog auditLog);

    // NhaNT | F07-05 - Lưu vào Database.
    Task<int> SaveChangesAsync();
}
