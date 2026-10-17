
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Implementations;

// =====================================================
// Developer: NhaNT
// UC-F07-04: Invoice Repository
// Truy vấn Invoice, InvoiceLine và ServiceUsage.
//
// SWAGGER: https://localhost:7024/swagger
// POST /api/stays/{stayId}/invoice/calculate
// POST /api/stays/{stayId}/invoice/finalize
// =====================================================

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;

    public InvoiceRepository(AppDbContext db)
    {
        _db = db;
    }

    // NhaNT | F07-04
    // Transaction Serializable bảo vệ quá trình tính phí.
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
    }

    // NhaNT | F07-04
    // Lấy nhân viên để kiểm tra quyền.
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    // NhaNT | F07-04
    // Lấy Stay, Reservation, Guest và Room.
    public Task<Stay?> GetStayAsync(int stayId)
    {
        return _db.Stays
            .Include(s => s.Reservation)
                .ThenInclude(r => r.Guest)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);
    }

    // NhaNT | F07-04
    // Chỉ lấy ServiceUsage chưa xóa mềm.
    public Task<List<ServiceUsage>> GetUsagesAsync(
        int stayId)
    {
        return _db.ServiceUsages
            .AsNoTracking()
            .Where(u =>
                u.StayId == stayId &&
                u.DeletedAt == null)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    // NhaNT | F07-04
    // Mỗi Stay có tối đa một Invoice.
    public Task<Invoice?> GetInvoiceAsync(int stayId)
    {
        return _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);
    }

    // NhaNT | F07-04
    // Không cho tính lại hoặc chốt sai quy trình.
    public Task<bool> HasPaymentsAsync(int invoiceId)
    {
        return _db.Payments
            .AnyAsync(p => p.InvoiceId == invoiceId);
    }

    // NhaNT | F07-04
    // Lấy dòng có tracking để đối chiếu hoặc xóa Draft.
    public Task<List<InvoiceLine>> GetLinesAsync(
        int invoiceId)
    {
        return _db.InvoiceLines
            .Where(l => l.InvoiceId == invoiceId)
            .ToListAsync();
    }

    // NhaNT | F07-04
    // Đọc kết quả hóa đơn không tracking.
    public Task<List<InvoiceLine>> GetLinesReadOnlyAsync(
        int invoiceId)
    {
        return _db.InvoiceLines
            .AsNoTracking()
            .Where(l => l.InvoiceId == invoiceId)
            .OrderBy(l => l.LineType)
            .ThenBy(l => l.Id)
            .ToListAsync();
    }

    // NhaNT | F07-04
    // Cộng cột Amount do SQL Server tính.
    public async Task<decimal> GetLineTotalAsync(
        int invoiceId)
    {
        return await _db.InvoiceLines
            .AsNoTracking()
            .Where(l => l.InvoiceId == invoiceId)
            .SumAsync(l => (decimal?)l.Amount) ?? 0m;
    }

    // NhaNT | F07-04
    // Thêm Invoice vào EF Core Change Tracker.
    public void AddInvoice(Invoice invoice)
    {
        _db.Invoices.Add(invoice);
    }

    // NhaNT | F07-04
    // Thêm một dòng chi phí phòng hoặc dịch vụ.
    public void AddLine(InvoiceLine line)
    {
        _db.InvoiceLines.Add(line);
    }

    // NhaNT | F07-04
    // Xóa dòng cũ để tính lại Invoice Draft.
    public void RemoveLines(
        IEnumerable<InvoiceLine> lines)
    {
        _db.InvoiceLines.RemoveRange(lines);
    }

    // NhaNT | F07-04
    // AuditLog được lưu cùng transaction.
    public void AddAuditLog(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
    }

    // NhaNT | F07-04
    // Lưu thay đổi thông qua AppDbContext.
    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
