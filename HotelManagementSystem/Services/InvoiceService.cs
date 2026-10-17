
using System.Text.Json;
using HotelManagementSystem.DTOs.Invoices;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNT
// UC-F07-04: Calculate & Finalize Invoice
// Service xử lý nghiệp vụ tính tiền và chốt hóa đơn.
//
// SWAGGER: https://localhost:7024/swagger
//
// TEST 1 - TÍNH HÓA ĐƠN:
// POST /api/stays/{stayId}/invoice/calculate
//
// TEST 2 - CHỐT HÓA ĐƠN:
// POST /api/stays/{stayId}/invoice/finalize
//
// Header: X-Demo-Account-Id (Development)
// Body: Không có (stayId truyền qua route).
// OData: Không sử dụng cho POST.
//
// Repository: IInvoiceRepository
// Mapper: InvoiceMapper
// =====================================================

public class InvoiceService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    // NhaNT | F07-04
    // Làm tròn tiền đến hai chữ số thập phân.
    private static decimal Money(decimal amount)
    {
        return Math.Round(
            amount, 2, MidpointRounding.AwayFromZero);
    }

    // NhaNT | F07-04
    // Chỉ Receptionist đang hoạt động được lập hóa đơn.
    private async Task<Account> GetReceptionistAsync(
        int accountId)
    {
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null || !account.IsActive)
        {
            throw new InvoiceBusinessException(
                403,
                "Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        if (account.Role != AccountRole.Receptionist)
        {
            throw new InvoiceBusinessException(
                403,
                "Chỉ Receptionist được xử lý hóa đơn.");
        }

        return account;
    }

    // NhaNT | F07-04
    // Chỉ lập hóa đơn cho Stay chưa check-out.
    private async Task<Stay> GetActiveStayAsync(int stayId)
    {
        var stay = await _repository.GetStayAsync(stayId);

        if (stay == null)
        {
            throw new InvoiceBusinessException(
                404, "Không tìm thấy Stay.");
        }

        if (stay.CheckedOutAt != null)
        {
            throw new InvoiceBusinessException(
                409, "Stay đã check-out.");
        }

        return stay;
    }

    // NhaNT | F07-04
    // Số đêm dựa trên lịch Reservation, tối thiểu 1.
    private static int GetNights(Reservation reservation)
    {
        return Math.Max(
            1,
            reservation.CheckOutDate.DayNumber -
            reservation.CheckInDate.DayNumber);
    }

    // NhaNT | F07-04
    // Dùng giá phòng đã chốt khi đặt phòng.
    private static decimal GetRoomAmount(
        Reservation reservation)
    {
        return Money(
            GetNights(reservation) * reservation.NightlyRate);
    }

    // NhaNT | F07-04
    // Đọc InvoiceLines và chuyển kết quả qua Mapper.
    private async Task<InvoiceResultDto> BuildResultAsync(
        Invoice invoice,
        string stayCode)
    {
        var lines =
            await _repository.GetLinesReadOnlyAsync(invoice.Id);

        return InvoiceMapper.ToResultDto(
            invoice,
            stayCode,
            lines);
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-04A: Calculate Invoice Draft
    //
    // SWAGGER: https://localhost:7024/swagger
    // Method: POST
    // Endpoint: /api/stays/{stayId}/invoice/calculate
    // Header: X-Demo-Account-Id
    // Input: stayId
    // Kết quả: Tạo/cập nhật Invoice Draft.
    // =================================================

    public async Task<InvoiceResultDto> CalculateAsync(
        int stayId,
        int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F07-04
        // Kiểm tra quyền và Stay.
        var account = await GetReceptionistAsync(accountId);
        var stay = await GetActiveStayAsync(stayId);
        var reservation = stay.Reservation;

        // NhaNT | F07-04
        // Kiểm tra ngày đặt và giá phòng.
        if (reservation.NightlyRate < 0 ||
            reservation.CheckOutDate <= reservation.CheckInDate)
        {
            throw new InvoiceBusinessException(
                409, "Dữ liệu đặt phòng không hợp lệ.");
        }

        // NhaNT | F07-04
        // Chỉ tính dịch vụ chưa bị xóa mềm.
        var usages = await _repository.GetUsagesAsync(stayId);

        if (usages.Any(u =>
            u.Quantity <= 0 || u.UnitPrice < 0))
        {
            throw new InvoiceBusinessException(
                409,
                "Có dữ liệu sử dụng dịch vụ không hợp lệ.");
        }

        // NhaNT | F07-04
        // Kiểm tra Invoice đã tồn tại hay chưa.
        var invoice = await _repository.GetInvoiceAsync(
            stayId);

        if (invoice != null &&
            invoice.Status == InvoiceStatus.Finalized)
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn đã Finalized, không thể tính lại.");
        }

        // NhaNT | F07-04
        // Không tính lại khi hóa đơn đã có Payment.
        if (invoice != null &&
            await _repository.HasPaymentsAsync(invoice.Id))
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn đã có thanh toán, không thể tính lại.");
        }

        var isNew = invoice == null;

        // NhaNT | F07-04
        // Tạo Invoice Draft nếu chưa tồn tại.
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

            _repository.AddInvoice(invoice);
        }

        // NhaNT | F07-04
        // Lưu thông tin snapshot cho hóa đơn.
        invoice.BillingFromDate =
            reservation.CheckInDate;

        invoice.BillingToDate =
            reservation.CheckOutDate;

        invoice.GuestNameSnapshot =
            reservation.Guest.FullName;

        invoice.GuestDocumentSnapshot =
            reservation.Guest.DocumentNumber;

        invoice.RoomCodeSnapshot = stay.Room.RoomCode;

        // Lưu trước để có InvoiceId.
        await _repository.SaveChangesAsync();

        // NhaNT | F07-04
        // Xóa InvoiceLines cũ nếu tính lại Draft.
        var oldLines =
            await _repository.GetLinesAsync(invoice.Id);

        if (oldLines.Count > 0)
        {
            _repository.RemoveLines(oldLines);
            await _repository.SaveChangesAsync();
        }

        // NhaNT | F07-04
        // Thêm một dòng tiền phòng.
        var nights = GetNights(reservation);

        _repository.AddLine(new InvoiceLine
        {
            InvoiceId = invoice.Id,
            LineType = InvoiceLineType.RoomCharge,
            ServiceUsageId = null,
            Description =
                $"Tiền phòng {stay.Room.RoomCode}",
            Unit = "dem",
            Quantity = nights,
            UnitPrice = reservation.NightlyRate
        });

        // NhaNT | F07-04
        // Thêm dòng phí từ ServiceUsage snapshot.
        foreach (var usage in usages)
        {
            _repository.AddLine(new InvoiceLine
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

        // SQL Server tính cột InvoiceLine.Amount.
        await _repository.SaveChangesAsync();

        // NhaNT | F07-04
        // Đồng bộ TotalAmount từ InvoiceLines.
        invoice.TotalAmount =
            await _repository.GetLineTotalAsync(invoice.Id);

        var expectedTotal =
            GetRoomAmount(reservation) +
            usages.Sum(u =>
                Money(u.Quantity * u.UnitPrice));

        if (invoice.TotalAmount != expectedTotal)
        {
            throw new InvoiceBusinessException(
                409,
                "Tổng tiền hóa đơn không khớp các khoản phí.");
        }

        // NhaNT | F07-04
        // Ghi AuditLog trong cùng transaction.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = isNew
                ? "CalculateInvoice"
                : "RecalculateInvoice",
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            EntityCodeSnapshot = invoice.InvoiceCode,
            OccurredAt = DateTime.UtcNow,
            Summary =
                $"Tính hóa đơn Draft cho Stay {stayId}.",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                invoice.TotalAmount,
                Nights = nights,
                ServiceCount = usages.Count
            })
        });

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BuildResultAsync(
            invoice, stay.StayCode);
    }

    // =================================================
    // Developer: NhaNT
    // UC-F07-04B: Finalize Invoice
    //
    // SWAGGER: https://localhost:7024/swagger
    // Method: POST
    // Endpoint: /api/stays/{stayId}/invoice/finalize
    // Header: X-Demo-Account-Id
    // Input: stayId
    // Kết quả: Chốt Invoice, khóa sửa chi phí.
    // =================================================

    public async Task<InvoiceResultDto> FinalizeAsync(
        int stayId,
        int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F07-04
        // Kiểm tra Receptionist và Stay.
        var account = await GetReceptionistAsync(accountId);
        var stay = await GetActiveStayAsync(stayId);

        // NhaNT | F07-04
        // Invoice Draft phải tồn tại.
        var invoice =
            await _repository.GetInvoiceAsync(stayId);

        if (invoice == null)
        {
            throw new InvoiceBusinessException(
                404,
                "Chưa có hóa đơn Draft. Hãy Calculate trước.");
        }

        if (invoice.Status == InvoiceStatus.Finalized)
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn đã được chốt trước đó.");
        }

        // NhaNT | F07-04
        // Draft không được có thanh toán.
        if (await _repository.HasPaymentsAsync(invoice.Id))
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn Draft đã có thanh toán bất thường.");
        }

        // NhaNT | F07-04
        // Kiểm tra số dòng phí phòng.
        var lines =
            await _repository.GetLinesAsync(invoice.Id);

        var roomLines = lines
            .Where(l =>
                l.LineType == InvoiceLineType.RoomCharge)
            .ToList();

        if (roomLines.Count != 1 ||
            roomLines[0].ServiceUsageId != null)
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn thiếu hoặc trùng dòng tiền phòng.");
        }

        // NhaNT | F07-04
        // Đối chiếu giá và số đêm với Reservation.
        var reservation = stay.Reservation;
        var expectedNights = GetNights(reservation);

        if (invoice.BillingFromDate !=
                reservation.CheckInDate ||
            invoice.BillingToDate !=
                reservation.CheckOutDate ||
            roomLines[0].Quantity != expectedNights ||
            roomLines[0].UnitPrice !=
                reservation.NightlyRate)
        {
            throw new InvoiceBusinessException(
                409,
                "Dòng tiền phòng không khớp Reservation.");
        }

        // NhaNT | F07-04
        // Đối chiếu dịch vụ còn hiệu lực.
        var usages =
            await _repository.GetUsagesAsync(stayId);

        var serviceLines = lines
            .Where(l =>
                l.LineType == InvoiceLineType.ServiceCharge)
            .ToList();

        if (serviceLines.Count != usages.Count)
        {
            throw new InvoiceBusinessException(
                409,
                "Hóa đơn chưa đồng bộ số lượng dịch vụ.");
        }

        foreach (var usage in usages)
        {
            var matches = serviceLines
                .Where(l =>
                    l.ServiceUsageId == usage.Id)
                .ToList();

            if (matches.Count != 1 ||
                matches[0].Quantity != usage.Quantity ||
                matches[0].UnitPrice != usage.UnitPrice ||
                matches[0].Description !=
                    usage.ServiceNameSnapshot ||
                matches[0].Unit != usage.UnitSnapshot)
            {
                throw new InvoiceBusinessException(
                    409,
                    $"Chi phí dịch vụ ID {usage.Id} " +
                    "không khớp hóa đơn.");
            }
        }

        // NhaNT | F07-04
        // Tổng Invoice phải khớp tổng dòng và snapshot.
        var lineTotal =
            await _repository.GetLineTotalAsync(invoice.Id);

        var expectedTotal =
            GetRoomAmount(reservation) +
            usages.Sum(u =>
                Money(u.Quantity * u.UnitPrice));

        if (invoice.TotalAmount != lineTotal ||
            lineTotal != expectedTotal)
        {
            throw new InvoiceBusinessException(
                409,
                "Tổng tiền hóa đơn không khớp, " +
                "hãy Calculate lại.");
        }

        // NhaNT | F07-04
        // Chốt hóa đơn và lưu nhân viên thực hiện.
        invoice.Status = InvoiceStatus.Finalized;
        invoice.FinalizedAt = DateTime.UtcNow;
        invoice.FinalizedByAccountId = account.Id;

        // NhaNT | F07-04
        // Ghi AuditLog khi Finalize.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "FinalizeInvoice",
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            EntityCodeSnapshot = invoice.InvoiceCode,
            OccurredAt = DateTime.UtcNow,
            Summary =
                $"Chốt hóa đơn cho Stay {stayId}.",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                invoice.TotalAmount,
                invoice.FinalizedAt,
                invoice.FinalizedByAccountId
            })
        });

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BuildResultAsync(
            invoice, stay.StayCode);
    }
}

// =====================================================
// Developer: NhaNT
// UC-F07-04: Exception nghiệp vụ hóa đơn.
// =====================================================
public class InvoiceBusinessException : Exception
{
    public int StatusCode { get; }

    public InvoiceBusinessException(
        int statusCode,
        string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
