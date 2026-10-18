
using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Interfaces;

// =====================================================
// Developer: NhaNT
// UC-F07-04: Calculate & Finalize Invoice
// Repository quản lý dữ liệu hóa đơn.
//
// TEST SWAGGER: https://localhost:7024/swagger
// POST /api/stays/{stayId}/invoice/calculate
// POST /api/stays/{stayId}/invoice/finalize
//
// Header: X-Demo-Account-Id (Development)
// =====================================================

public interface IInvoiceRepository
{
    // NhaNT | F07-04 - Transaction chung.
    Task<IDbContextTransaction> BeginTransactionAsync();

    // NhaNT | F07-04 - Tài khoản thực hiện.
    Task<Account?> GetAccountAsync(int accountId);

    // NhaNT | F07-04 - Stay cùng dữ liệu liên quan.
    Task<Stay?> GetStayAsync(int stayId);

    // NhaNT | F07-04 - Lượt sử dụng còn hiệu lực.
    Task<List<ServiceUsage>> GetUsagesAsync(int stayId);

    // NhaNT | F07-04 - Hóa đơn của Stay.
    Task<Invoice?> GetInvoiceAsync(int stayId);

    // NhaNT | F07-04 - Kiểm tra đã thanh toán.
    Task<bool> HasPaymentsAsync(int invoiceId);

    // NhaNT | F07-04 - Lấy dòng hóa đơn.
    Task<List<InvoiceLine>> GetLinesAsync(int invoiceId);

    // NhaNT | F07-04 - Đọc dòng phí không tracking.
    Task<List<InvoiceLine>> GetLinesReadOnlyAsync(int invoiceId);

    // NhaNT | F07-04 - Tổng tiền từ SQL Server.
    Task<decimal> GetLineTotalAsync(int invoiceId);

    // NhaNT | F07-04 - Thêm hóa đơn.
    void AddInvoice(Invoice invoice);

    // NhaNT | F07-04 - Thêm dòng chi phí.
    void AddLine(InvoiceLine line);

    // NhaNT | F07-04 - Xóa dòng hóa đơn nháp.
    void RemoveLines(IEnumerable<InvoiceLine> lines);

    // NhaNT | F07-04 - Ghi lịch sử thao tác.
    void AddAuditLog(AuditLog auditLog);

    // NhaNT | F07-04 - Lưu dữ liệu.
    Task<int> SaveChangesAsync();
}
