
using System.Data;
using System.Text.Json;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.Invoices;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-04: Calculate & Finalize Invoice
// - Chỉ Receptionist được thao tác.
// - Giá phòng và dịch vụ lấy từ dữ liệu lịch sử.
// - Không tính ServiceUsage đã soft-delete.
// - Transaction bảo vệ Invoice + InvoiceLines + AuditLog.
// - Invoice Finalized không được tính lại hoặc chốt lại.
// =====================================================

public class InvoiceService
{
    private readonly AppDbContext _db;

    public InvoiceService(AppDbContext db)
    {
        _db = db;
    }

    // Chuẩn hóa số tiền đến 2 chữ số thập phân.
    private static decimal Money(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private async Task<Account> GetReceptionistAsync(int accountId)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null || !account.IsActive)
            throw new InvoiceBusinessException(
                403, "Tài khoản không tồn tại hoặc đã bị khóa.");

        if (account.Role != AccountRole.Receptionist)
            throw new InvoiceBusinessException(
                403, "Chỉ Receptionist được xử lý hóa đơn.");

        return account;
    }

    private async Task<Stay> GetActiveStayAsync(int stayId)
    {
        var stay = await _db.Stays
            .Include(s => s.Reservation)
                .ThenInclude(r => r.Guest)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay == null)
            throw new InvoiceBusinessException(
                404, "Không tìm thấy Stay.");

        if (stay.CheckedOutAt != null)
            throw new InvoiceBusinessException(
                409, "Stay đã check-out.");

        return stay;
    }

    // Chỉ dùng ngày đặt phòng làm kỳ tính phí cho bản demo.
    private static int GetNights(Reservation reservation)
    {
        return Math.Max(1,
            reservation.CheckOutDate.DayNumber -
            reservation.CheckInDate.DayNumber);
    }

    private static decimal GetRoomAmount(Reservation reservation)
    {
        return Money(GetNights(reservation) * reservation.NightlyRate);
    }

    // Tạo DTO từ hóa đơn và các dòng đã lưu trong SQL.
    private async Task<InvoiceResultDto> BuildResultAsync(
        Invoice invoice,
        string stayCode)
    {
        var lines = await _db.InvoiceLines
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoice.Id)
            .OrderBy(x => x.LineType)
            .ThenBy(x => x.Id)
            .Select(x => new InvoiceLineResultDto
            {
                Id = x.Id,
                LineType = (int)x.LineType == 1
                    ? "RoomCharge"
                    : "ServiceCharge",
                ServiceUsageId = x.ServiceUsageId,
                Description = x.Description,
                Unit = x.Unit,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Amount = x.Amount
            })
            .ToListAsync();

        return new InvoiceResultDto
        {
            Id = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            StayId = invoice.StayId,
            StayCode = stayCode,
            Status = (int)invoice.Status == 2
                ? "Finalized" : "Draft",
            BillingFromDate = invoice.BillingFromDate,
            BillingToDate = invoice.BillingToDate,
            GuestName = invoice.GuestNameSnapshot,
            RoomCode = invoice.RoomCodeSnapshot,
            RoomAmount = lines
                .Where(x => x.LineType == "RoomCharge")
                .Sum(x => x.Amount),
            ServiceAmount = lines
                .Where(x => x.LineType == "ServiceCharge")
                .Sum(x => x.Amount),
            TotalAmount = invoice.TotalAmount,
            FinalizedAt = invoice.FinalizedAt,
            Lines = lines
        };
    }

    // =================================================
    // UC-F07-04A: CALCULATE INVOICE DRAFT
    // POST /api/stays/{stayId}/invoice/calculate
    // =================================================

    public async Task<InvoiceResultDto> CalculateAsync(
        int stayId, int accountId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await GetReceptionistAsync(accountId);
        var stay = await GetActiveStayAsync(stayId);
        var reservation = stay.Reservation;

        if (reservation.NightlyRate < 0 ||
            reservation.CheckOutDate <= reservation.CheckInDate)
        {
            throw new InvoiceBusinessException(
                409, "Dữ liệu đặt phòng không hợp lệ.");
        }

        // Dịch vụ đã xóa mềm không được đưa vào hóa đơn.
        var usages = await _db.ServiceUsages
            .Where(u => u.StayId == stayId && u.DeletedAt == null)
            .OrderBy(u => u.Id)
            .ToListAsync();

        if (usages.Any(u => u.Quantity <= 0 || u.UnitPrice < 0))
            throw new InvoiceBusinessException(
                409, "Có dữ liệu sử dụng dịch vụ không hợp lệ.");

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);

        if (invoice != null && (int)invoice.Status == 2)
            throw new InvoiceBusinessException(
                409, "Hóa đơn đã Finalized, không thể tính lại.");

        // Không cho lập lại Draft nếu đã ghi nhận thanh toán.
        if (invoice != null &&
            await _db.Payments.AnyAsync(p => p.InvoiceId == invoice.Id))
        {
            throw new InvoiceBusinessException(
                409, "Hóa đơn đã có thanh toán, không thể tính lại.");
        }

        bool isNew = invoice == null;

        if (invoice == null)
        {
            invoice = new Invoice
            {
                InvoiceCode = "INV-" +
                    Guid.NewGuid().ToString("N")[..20],
                StayId = stayId,
                Status = InvoiceStatus.Draft,
                CreatedByAccountId = account.Id,
                TotalAmount = 0m
            };

            _db.Invoices.Add(invoice);
        }

        invoice.BillingFromDate = reservation.CheckInDate;
        invoice.BillingToDate = reservation.CheckOutDate;
        invoice.GuestNameSnapshot = reservation.Guest.FullName;
        invoice.GuestDocumentSnapshot = reservation.Guest.DocumentNumber;
        invoice.RoomCodeSnapshot = stay.Room.RoomCode;

        // Lưu hóa đơn trước để có InvoiceId.
        await _db.SaveChangesAsync();

        // Đồng bộ lại toàn bộ dòng phí của Draft.
        // Dòng đã xóa mềm sẽ không được thêm trở lại.
        var oldLines = await _db.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .ToListAsync();

        if (oldLines.Count > 0)
        {
            _db.InvoiceLines.RemoveRange(oldLines);
            await _db.SaveChangesAsync();
        }

        var nights = GetNights(reservation);

        _db.InvoiceLines.Add(new InvoiceLine
        {
            InvoiceId = invoice.Id,
            LineType = InvoiceLineType.RoomCharge,
            ServiceUsageId = null,
            Description = $"Tiền phòng {stay.Room.RoomCode}",
            Unit = "dem",
            Quantity = nights,
            UnitPrice = reservation.NightlyRate
        });

        foreach (var usage in usages)
        {
            _db.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                LineType = InvoiceLineType.ServiceCharge,
                ServiceUsageId = usage.Id,
                Description = usage.ServiceNameSnapshot,
                Unit = usage.UnitSnapshot,
                Quantity = usage.Quantity,
                UnitPrice = usage.UnitPrice
            });
        }

        // SQL Server tính computed column Amount.
        await _db.SaveChangesAsync();

        invoice.TotalAmount = await _db.InvoiceLines
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoice.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var expectedTotal = GetRoomAmount(reservation)
            + usages.Sum(x => Money(x.Quantity * x.UnitPrice));

        if (invoice.TotalAmount != expectedTotal)
            throw new InvoiceBusinessException(
                409, "Tổng tiền hóa đơn không khớp các khoản phí.");

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = isNew ? "CalculateInvoice" : "RecalculateInvoice",
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            EntityCodeSnapshot = invoice.InvoiceCode,
            OccurredAt = DateTime.UtcNow,
            Summary = $"Tính hóa đơn Draft cho Stay {stayId}.",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                invoice.TotalAmount,
                Nights = nights,
                ServiceCount = usages.Count
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BuildResultAsync(invoice, stay.StayCode);
    }

    // =================================================
    // UC-F07-04B: FINALIZE INVOICE
    // POST /api/stays/{stayId}/invoice/finalize
    // =================================================

    public async Task<InvoiceResultDto> FinalizeAsync(
        int stayId, int accountId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await GetReceptionistAsync(accountId);
        var stay = await GetActiveStayAsync(stayId);

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);

        if (invoice == null)
            throw new InvoiceBusinessException(
                404, "Chưa có hóa đơn Draft. Hãy Calculate trước.");

        if ((int)invoice.Status == 2)
            throw new InvoiceBusinessException(
                409, "Hóa đơn đã được chốt trước đó.");

        // Không chốt nếu đã có thanh toán không đúng quy trình.
        if (await _db.Payments.AnyAsync(p => p.InvoiceId == invoice.Id))
            throw new InvoiceBusinessException(
                409, "Hóa đơn Draft đã có thanh toán bất thường.");

        // Kiểm tra dòng tiền phòng.
        var lines = await _db.InvoiceLines
            .Where(x => x.InvoiceId == invoice.Id)
            .ToListAsync();

        var roomLines = lines
            .Where(x => (int)x.LineType == 1)
            .ToList();

        if (roomLines.Count != 1 ||
            roomLines[0].ServiceUsageId != null)
            throw new InvoiceBusinessException(
                409, "Hóa đơn thiếu hoặc trùng dòng tiền phòng.");

        var reservation = stay.Reservation;
        var expectedNights = GetNights(reservation);

        if (invoice.BillingFromDate != reservation.CheckInDate ||
            invoice.BillingToDate != reservation.CheckOutDate ||
            roomLines[0].Quantity != expectedNights ||
            roomLines[0].UnitPrice != reservation.NightlyRate)
        {
            throw new InvoiceBusinessException(
                409, "Dòng tiền phòng không khớp Reservation.");
        }

        // Kiểm tra mọi ServiceUsage còn hiệu lực.
        var usages = await _db.ServiceUsages
            .Where(x => x.StayId == stayId && x.DeletedAt == null)
            .ToListAsync();

        var serviceLines = lines
            .Where(x => (int)x.LineType == 2)
            .ToList();

        if (serviceLines.Count != usages.Count)
            throw new InvoiceBusinessException(
                409, "Hóa đơn chưa đồng bộ số lượng dịch vụ.");

        foreach (var usage in usages)
        {
            var matches = serviceLines
                .Where(x => x.ServiceUsageId == usage.Id)
                .ToList();

            if (matches.Count != 1 ||
                matches[0].Quantity != usage.Quantity ||
                matches[0].UnitPrice != usage.UnitPrice ||
                matches[0].Description != usage.ServiceNameSnapshot ||
                matches[0].Unit != usage.UnitSnapshot)
            {
                throw new InvoiceBusinessException(
                    409,
                    $"Chi phí dịch vụ ID {usage.Id} không khớp hóa đơn.");
            }
        }

        // Đối chiếu tổng tiền được SQL Server tính.
        var lineTotal = await _db.InvoiceLines
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoice.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var expectedTotal = GetRoomAmount(reservation)
            + usages.Sum(x => Money(x.Quantity * x.UnitPrice));

        if (invoice.TotalAmount != lineTotal ||
            lineTotal != expectedTotal)
            throw new InvoiceBusinessException(
                409, "Tổng tiền hóa đơn không khớp, hãy Calculate lại.");

        invoice.Status = InvoiceStatus.Finalized;
        invoice.FinalizedAt = DateTime.UtcNow;
        invoice.FinalizedByAccountId = account.Id;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "FinalizeInvoice",
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            EntityCodeSnapshot = invoice.InvoiceCode,
            OccurredAt = DateTime.UtcNow,
            Summary = $"Chốt hóa đơn cho Stay {stayId}.",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                invoice.TotalAmount,
                invoice.FinalizedAt,
                invoice.FinalizedByAccountId
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BuildResultAsync(invoice, stay.StayCode);
    }
}

// =====================================================
// Developer: NhaNTCE181298
// UC-F07-04: Lỗi nghiệp vụ hóa đơn.
// =====================================================
public class InvoiceBusinessException : Exception
{
    public int StatusCode { get; }

    public InvoiceBusinessException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
