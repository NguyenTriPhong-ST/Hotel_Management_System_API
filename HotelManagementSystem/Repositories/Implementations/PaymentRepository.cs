
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelManagementSystem.Repositories.Implementations;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-05: Payment Repository
// Truy vấn và lưu dữ liệu Payment bằng EF Core.
//
// Swagger: https://localhost:7024/swagger
// POST /api/invoices/{invoiceId}/payments
// =====================================================

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    // NhaNT | F07-05
    // Bảo vệ giao dịch thanh toán bằng Serializable.
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
    }

    // NhaNT | F07-05
    // Lấy tài khoản để Service kiểm tra quyền.
    public Task<Account?> GetAccountAsync(int accountId)
    {
        return _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    // NhaNT | F07-05
    // Kiểm tra IdempotencyKey trên toàn hệ thống.
    public Task<Payment?> GetByIdempotencyKeyAsync(Guid key)
    {
        return _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.IdempotencyKey == key);
    }

    // NhaNT | F07-05
    // Lấy Invoice để kiểm tra trạng thái và tổng tiền.
    public Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        return _db.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    // NhaNT | F07-05
    // Cộng số tiền đã thanh toán cho Invoice.
    public async Task<decimal> GetTotalPaidAsync(
        int invoiceId)
    {
        return await _db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
    }

    // NhaNT | F07-05
    // Thêm Payment vào EF Core Change Tracker.
    public void AddPayment(Payment payment)
    {
        _db.Payments.Add(payment);
    }

    // NhaNT | F07-05
    // Ghi AuditLog trong cùng transaction.
    public void AddAuditLog(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
    }

    // NhaNT | F07-05
    // Lưu thay đổi vào SQL Server.
    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
