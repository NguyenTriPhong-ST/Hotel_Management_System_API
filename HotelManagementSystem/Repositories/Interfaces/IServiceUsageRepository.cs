
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Interfaces;

// =====================================================
// Developer: NhaNTCE181298
// Module: F09 - Service Usage Management
// Layer: Repository Interface
//
// Swagger: https://localhost:7024/swagger
// =====================================================

public interface IServiceUsageRepository
{
    // NhaNT | F09-01, 04, 05 - Transaction.
    Task<IDbContextTransaction> BeginTransactionAsync();

    // NhaNT | F09-01, 04, 05 - Kiểm tra nhân viên.
    Task<Account?> GetAccountAsync(int accountId);

    // NhaNT | F09-01 - Kiểm tra Stay.
    Task<Stay?> GetStayAsync(int stayId);

    // NhaNT | F09-01 - Kiểm tra Invoice Finalized.
    Task<bool> HasFinalizedInvoiceAsync(int stayId);

    // NhaNT | F09-01 - Lấy dịch vụ khách sạn.
    Task<HotelService?> GetHotelServiceAsync(int serviceId);

    // NhaNT | F09-01 - Thêm lượt sử dụng.
    void AddUsage(ServiceUsage usage);

    IQueryable<ServiceUsageListDto> QueryUsageViews();

    // NhaNT | F09-03 - Chi tiết lượt sử dụng.
    Task<ServiceUsageDetailDto?> GetDetailAsync(int id);

    // NhaNT | F09-04, 05 - Lấy bản ghi để sửa/xóa.
    Task<ServiceUsage?> GetEditableUsageAsync(int id);

    // NhaNT | F09-04, 05 - Hóa đơn đang theo dõi.
    Task<Invoice?> GetInvoiceAsync(int stayId);

    // NhaNT | F09-04, 05 - Dòng phí dịch vụ.
    Task<InvoiceLine?> GetInvoiceLineAsync(
        int invoiceId, int usageId);

    // NhaNT | F09-04 - Tạo dòng phí còn thiếu.
    void AddInvoiceLine(InvoiceLine line);

    // NhaNT | F09-05 - Xóa dòng phí khi soft delete.
    void RemoveInvoiceLine(InvoiceLine line);

    // NhaNT | F09-04, 05 - Tổng tiền Invoice Draft.
    Task<decimal> GetInvoiceTotalAsync(int invoiceId);

    // NhaNT | F09-01, 04, 05 - Ghi AuditLog.
    void AddAuditLog(AuditLog log);

    // NhaNT | F09-01, 04, 05 - Lưu EF Core.
    Task<int> SaveChangesAsync();
}
