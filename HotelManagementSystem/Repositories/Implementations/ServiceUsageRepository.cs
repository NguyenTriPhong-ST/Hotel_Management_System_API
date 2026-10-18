
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Implementations;

// =====================================================
// Developer: NhaNTCE181298
// Module: F09 - Service Usage Management
// Layer: Repository
// Truy vấn ServiceUsage, InvoiceLine và AuditLog.
//
// Swagger: https://localhost:7024/swagger
//
//
// Danh sách:
//
// Lọc theo Stay:
//
// Sắp xếp mới nhất:
//
// Phân trang:
//
// Chọn trường:
// =====================================================

public class ServiceUsageRepository : IServiceUsageRepository
{
    private readonly AppDbContext _db;

    public ServiceUsageRepository(AppDbContext db)
    {
        _db = db;
    }

    // NhaNT | UC-F09-01, 04, 05
    // Giữ transaction Serializable như code cũ.
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
    }

    // NhaNT | UC-F09-01, 04, 05
    // Đọc tài khoản để Service kiểm tra quyền.
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    // NhaNT | UC-F09-01
    // Kiểm tra Stay đang lưu trú.
    public Task<Stay?> GetStayAsync(int stayId)
    {
        return _db.Stays
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stayId);
    }

    // NhaNT | UC-F09-01
    // Không cho thêm dịch vụ khi Invoice đã chốt.
    public Task<bool> HasFinalizedInvoiceAsync(int stayId)
    {
        return _db.Invoices
            .AnyAsync(i =>
                i.StayId == stayId &&
                (int)i.Status == 2);
    }

    // NhaNT | UC-F09-01
    // Lấy dịch vụ và đơn giá hiện tại.
    public Task<HotelService?> GetHotelServiceAsync(
        int serviceId)
    {
        return _db.HotelServices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId);
    }

    // NhaNT | UC-F09-01
    // Tạo ServiceUsage và giữ snapshot giá.
    public void AddUsage(ServiceUsage usage)
    {
        _db.ServiceUsages.Add(usage);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-02: View Service Usages
    //
    //
    // Chỉ lấy bản ghi chưa bị xóa mềm.
    // =================================================
    public IQueryable<ServiceUsageListDto> QueryUsageViews()
    {
        return _db.ServiceUsages
            .AsNoTracking()
            .Where(su => su.DeletedAt == null)
            .Select(ServiceUsageMapper.ToListDto);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-03: View Service Usage Details
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/service-usages/{id}
    // Input: ID của ServiceUsage.
    // =================================================
    public Task<ServiceUsageDetailDto?> GetDetailAsync(int id)
    {
        return (
            from su in _db.ServiceUsages.AsNoTracking()

            join st in _db.Stays.AsNoTracking()
                on su.StayId equals st.Id

            join r in _db.Rooms.AsNoTracking()
                on st.RoomId equals r.Id

            join a in _db.Accounts.AsNoTracking()
                on su.RecordedByAccountId equals a.Id

            where su.Id == id && su.DeletedAt == null

            select new ServiceUsageDetailDto
            {
                Id = su.Id,
                StayId = su.StayId,
                StayCode = st.StayCode,

                RoomId = st.RoomId,
                RoomCode = r.RoomCode,

                HotelServiceId = su.HotelServiceId,
                ServiceName = su.ServiceNameSnapshot,
                Unit = su.UnitSnapshot,

                Quantity = su.Quantity,
                UnitPrice = su.UnitPrice,
                Amount = su.Quantity * su.UnitPrice,

                UsedAt = su.UsedAt,
                RecordedByAccountId = su.RecordedByAccountId,
                RecordedByName = a.FullName,

                Notes = su.Notes,
                CreatedAt = su.CreatedAt,
                UpdatedAt = su.UpdatedAt
            }
        ).FirstOrDefaultAsync();
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Lấy bản ghi có tracking để cập nhật.
    public Task<ServiceUsage?> GetEditableUsageAsync(int id)
    {
        return _db.ServiceUsages
            .Include(su => su.Stay)
            .FirstOrDefaultAsync(su => su.Id == id);
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Lấy Invoice Draft nếu đã được Calculate.
    public Task<Invoice?> GetInvoiceAsync(int stayId)
    {
        return _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Dòng hóa đơn tương ứng với ServiceUsage.
    public Task<InvoiceLine?> GetInvoiceLineAsync(
        int invoiceId,
        int usageId)
    {
        return _db.InvoiceLines
            .FirstOrDefaultAsync(l =>
                l.InvoiceId == invoiceId &&
                l.ServiceUsageId == usageId);
    }

    // NhaNT | UC-F09-04
    // Thêm InvoiceLine nếu hóa đơn nháp còn thiếu.
    public void AddInvoiceLine(InvoiceLine line)
    {
        _db.InvoiceLines.Add(line);
    }

    // NhaNT | UC-F09-05
    // Xóa InvoiceLine, không xóa ServiceUsage vật lý.
    public void RemoveInvoiceLine(InvoiceLine line)
    {
        _db.InvoiceLines.Remove(line);
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Lấy tổng Amount computed column sau SaveChanges.
    public async Task<decimal> GetInvoiceTotalAsync(
        int invoiceId)
    {
        return await _db.InvoiceLines
            .AsNoTracking()
            .Where(l => l.InvoiceId == invoiceId)
            .SumAsync(l => (decimal?)l.Amount) ?? 0m;
    }

    // NhaNT | UC-F09-01, 04, 05
    // AuditLog được lưu trong cùng transaction.
    public void AddAuditLog(AuditLog log)
    {
        _db.AuditLogs.Add(log);
    }

    // NhaNT | UC-F09-01, 04, 05
    // Ghi dữ liệu vào SQL Server.
    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
