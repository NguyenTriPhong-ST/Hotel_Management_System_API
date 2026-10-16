
using System.Data;
using HotelManagementSystem.Data;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// UC: UC-F09-01 - Create Service Usage
// Mô tả: Ghi nhận dịch vụ trong thời gian lưu trú.
// OData: Không áp dụng cho thao tác tạo dữ liệu.
// =====================================================

public class ServiceUsageService
{
    private readonly AppDbContext _db;

    public ServiceUsageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateServiceUsageResponse> CreateAsync(
        CreateServiceUsageRequest request,
        int accountId)
    {
        var now = DateTime.UtcNow;

        // Transaction đảm bảo ServiceUsage và AuditLog
        // cùng được lưu hoặc cùng rollback.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        // 1. Kiểm tra nhân viên lễ tân.
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null || !account.IsActive)
        {
            throw new ServiceUsageBusinessException(
                403, "Tài khoản không hợp lệ hoặc đã bị khóa.");
        }

        if (account.Role != AccountRole.Receptionist)
        {
            throw new ServiceUsageBusinessException(
                403, "Tài khoản không có quyền ghi nhận dịch vụ.");
        }

        // 2. Kiểm tra Stay còn hoạt động.
        var stay = await _db.Stays
            .FirstOrDefaultAsync(s => s.Id == request.StayId);

        if (stay == null)
        {
            throw new ServiceUsageBusinessException(
                404, "Không tìm thấy Stay.");
        }

        if (stay.CheckedOutAt != null)
        {
            throw new ServiceUsageBusinessException(
                409, "Stay đã check-out, không thể thêm dịch vụ.");
        }

        // 3. Kiểm tra hóa đơn chưa Finalized.
        // Invoice.Status: 1 = Draft, 2 = Finalized.
        var invoiceFinalized = await _db.Invoices
            .AnyAsync(i =>
                i.StayId == request.StayId &&
                (int)i.Status == 2);

        if (invoiceFinalized)
        {
            throw new ServiceUsageBusinessException(
                409, "Hóa đơn đã chốt, không thể thêm dịch vụ.");
        }

        // 4. Kiểm tra dịch vụ tồn tại và hoạt động.
        var hotelService = await _db.HotelServices
            .FirstOrDefaultAsync(s =>
                s.Id == request.HotelServiceId);

        if (hotelService == null)
        {
            throw new ServiceUsageBusinessException(
                404, "Không tìm thấy dịch vụ.");
        }

        if (!hotelService.IsActive)
        {
            throw new ServiceUsageBusinessException(
                409, "Dịch vụ hiện không hoạt động.");
        }

        // 5. Kiểm tra số lượng.
        if (request.Quantity <= 0)
        {
            throw new ServiceUsageBusinessException(
                400, "Số lượng sử dụng phải lớn hơn 0.");
        }

        // 6. Tạo ServiceUsage.
        // Lưu snapshot để khi giá danh mục thay đổi,
        // lịch sử chi phí vẫn giữ đúng đơn giá cũ.
        var usage = new ServiceUsage
        {
            StayId = stay.Id,
            HotelServiceId = hotelService.Id,

            ServiceNameSnapshot = hotelService.Name,
            UnitSnapshot = hotelService.Unit,
            UnitPrice = hotelService.CurrentPrice,

            Quantity = request.Quantity,
            UsedAt = now,
            RecordedByAccountId = account.Id,
            Notes = request.Notes?.Trim()
        };

        _db.ServiceUsages.Add(usage);

        await _db.SaveChangesAsync();

        // 7. Ghi AuditLog tạm thời.
        // Khi nhóm hoàn thành module LOG-01,
        // chuyển phần này sang AuditLogService chung.
        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "CreateServiceUsage",
            EntityType = "ServiceUsage",
            EntityId = usage.Id.ToString(),
            OccurredAt = now,
            Summary =
                $"Ghi nhận {usage.Quantity} {usage.UnitSnapshot} " +
                $"{usage.ServiceNameSnapshot} cho Stay {stay.StayCode}."
        });

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        // 8. Trả kết quả cho Swagger.
        return new CreateServiceUsageResponse
        {
            Id = usage.Id,
            StayId = usage.StayId,
            HotelServiceId = usage.HotelServiceId,
            ServiceName = usage.ServiceNameSnapshot,
            Unit = usage.UnitSnapshot,
            Quantity = usage.Quantity,
            UnitPrice = usage.UnitPrice,
            Amount = usage.Quantity * usage.UnitPrice,
            UsedAt = usage.UsedAt,
            RecordedByAccountId = usage.RecordedByAccountId,
            Notes = usage.Notes,
            Message = "Ghi nhận dịch vụ thành công."
        };
    }


    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F09-02 - View Service Usages
    // OData: IQueryable cho phép truy vấn động trên SQL.
    // Mô tả: Xem danh sách lượt sử dụng chưa bị xóa mềm.
    // =====================================================

    public IQueryable<ServiceUsageListDto> GetServiceUsages()
    {
        return _db.ServiceUsages
            .AsNoTracking()

            // Không hiển thị dịch vụ đã xóa mềm.
            .Where(su => su.DeletedAt == null)

            // OData áp dụng filter, orderby, skip, top
            // vào IQueryable trước khi EF Core truy vấn SQL.
            .Select(su => new ServiceUsageListDto
            {
                Id = su.Id,

                StayId = su.StayId,
                StayCode = su.Stay.StayCode,

                RoomId = su.Stay.RoomId,
                RoomCode = su.Stay.Room.RoomCode,

                HotelServiceId = su.HotelServiceId,

                // Sử dụng snapshot, không lấy giá hiện tại.
                ServiceName = su.ServiceNameSnapshot,
                Unit = su.UnitSnapshot,

                Quantity = su.Quantity,
                UnitPrice = su.UnitPrice,
                Amount = su.Quantity * su.UnitPrice,

                UsedAt = su.UsedAt,
                RecordedByAccountId = su.RecordedByAccountId,
                Notes = su.Notes
            });
    }

    // =====================================================
    // Developer: NhaNTCE181298
    // UC: UC-F09-03 - View Service Usage Details
    // Mô tả: Lấy chi tiết lượt sử dụng dịch vụ theo ID.
    // OData: Không dùng vì truy vấn một bản ghi.
    // =====================================================

    public async Task<ServiceUsageDetailDto?> GetDetailAsync(int id)
    {
        return await (
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


    // =====================================================
    // Developer: NhaNTCE181298
    // UC-F09-04: Update Service Usage
    // UC-F09-05: Soft Delete Service Usage
    // OData: Không áp dụng cho PUT/DELETE.
    // =====================================================

    // NhaNTCE181298 | F09-04, F09-05
    // Kiểm tra quyền Receptionist theo tài khoản demo.
    // Chỉ dùng trong Development; sau này thay bằng JWT.
    private async Task<Account> CheckReceptionistAsync(int accountId)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null || !account.IsActive)
            throw new ServiceUsageBusinessException(
                403, "Tài khoản không hợp lệ hoặc đã bị khóa.");

        if (account.Role != AccountRole.Receptionist)
            throw new ServiceUsageBusinessException(
                403, "Tài khoản không có quyền quản lý dịch vụ.");

        return account;
    }

    // NhaNTCE181298 | F09-04, F09-05
    // Kiểm tra bản ghi tồn tại và chưa bị xóa mềm.
    private async Task<ServiceUsage> GetEditableUsageAsync(int id)
    {
        var usage = await _db.ServiceUsages
            .Include(x => x.Stay)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (usage == null || usage.DeletedAt != null)
            throw new ServiceUsageBusinessException(
                404, "Không tìm thấy lượt sử dụng dịch vụ.");

        if (usage.Stay.CheckedOutAt != null)
            throw new ServiceUsageBusinessException(
                409, "Stay đã check-out, không thể chỉnh sửa dịch vụ.");

        return usage;
    }

    // NhaNTCE181298 | F09-04, F09-05
    // Lấy Invoice và không cho thay đổi khi đã Finalized.
    private async Task<Invoice?> GetEditableInvoiceAsync(int stayId)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.StayId == stayId);

        if (invoice != null && (int)invoice.Status == 2)
            throw new ServiceUsageBusinessException(
                409, "Hóa đơn đã chốt, không thể chỉnh sửa dịch vụ.");

        return invoice;
    }

    // NhaNTCE181298 | F09-04, F09-05
    // Đồng bộ tổng tiền hóa đơn sau khi InvoiceLine thay đổi.
    // InvoiceLine.Amount là computed column từ SQL Server.
    // Phải gọi sau SaveChangesAsync trong cùng transaction.
    private async Task RecalculateDraftInvoiceAsync(Invoice invoice)
    {
        var total = await _db.InvoiceLines
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoice.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        invoice.TotalAmount = total;
    }

    // =====================================================
    // UC-F09-04 - UPDATE SERVICE USAGE
    // PUT /api/service-usages/{id}
    // =====================================================

    public async Task<CreateServiceUsageResponse> UpdateAsync(
        int id,
        UpdateServiceUsageRequest request,
        int accountId)
    {
        // Transaction: ServiceUsage, InvoiceLine, Invoice
        // và AuditLog phải cập nhật cùng nhau.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await CheckReceptionistAsync(accountId);
        var usage = await GetEditableUsageAsync(id);
        var invoice = await GetEditableInvoiceAsync(usage.StayId);

        if (request.Quantity <= 0)
            throw new ServiceUsageBusinessException(
                400, "Quantity phải lớn hơn 0.");

        var oldQuantity = usage.Quantity;
        var oldNotes = usage.Notes;

        // Chỉ sửa số lượng và ghi chú.
        // Giữ nguyên UnitPrice và các snapshot lịch sử.
        usage.Quantity = request.Quantity;
        usage.Notes = request.Notes?.Trim();

        // Nếu Invoice Draft tồn tại, cập nhật dòng dịch vụ.
        if (invoice != null)
        {
            var line = await _db.InvoiceLines
                .FirstOrDefaultAsync(x =>
                    x.InvoiceId == invoice.Id &&
                    x.ServiceUsageId == usage.Id);

            if (line == null)
            {
                // Bổ sung dòng còn thiếu trong hóa đơn nháp.
                line = new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    ServiceUsageId = usage.Id,
                    LineType = (InvoiceLineType)2,
                    Description = usage.ServiceNameSnapshot,
                    Unit = usage.UnitSnapshot,
                    Quantity = usage.Quantity,
                    UnitPrice = usage.UnitPrice
                };

                _db.InvoiceLines.Add(line);
            }
            else
            {
                line.Quantity = usage.Quantity;

                // Bảo đảm khớp snapshot của ServiceUsage.
                line.UnitPrice = usage.UnitPrice;
                line.Description = usage.ServiceNameSnapshot;
                line.Unit = usage.UnitSnapshot;
            }
        }

        // Lưu trước để SQL Server tính lại InvoiceLine.Amount.
        await _db.SaveChangesAsync();

        if (invoice != null)
        {
            await RecalculateDraftInvoiceAsync(invoice);
        }

        var now = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "UpdateServiceUsage",
            EntityType = "ServiceUsage",
            EntityId = usage.Id.ToString(),
            OccurredAt = now,
            Summary = $"Cập nhật dịch vụ ID {usage.Id}.",
            OldValuesJson = JsonSerializer.Serialize(new
            {
                Quantity = oldQuantity,
                Notes = oldNotes
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Quantity = usage.Quantity,
                Notes = usage.Notes
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CreateServiceUsageResponse
        {
            Id = usage.Id,
            StayId = usage.StayId,
            HotelServiceId = usage.HotelServiceId,
            ServiceName = usage.ServiceNameSnapshot,
            Unit = usage.UnitSnapshot,
            Quantity = usage.Quantity,
            UnitPrice = usage.UnitPrice,
            Amount = usage.Quantity * usage.UnitPrice,
            UsedAt = usage.UsedAt,
            RecordedByAccountId = usage.RecordedByAccountId,
            Notes = usage.Notes,
            Message = "Cập nhật lượt sử dụng dịch vụ thành công."
        };
    }

    // =====================================================
    // UC-F09-05 - SOFT DELETE SERVICE USAGE
    // DELETE /api/service-usages/{id}
    // =====================================================

    public async Task DeleteAsync(int id, int accountId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await CheckReceptionistAsync(accountId);
        var usage = await GetEditableUsageAsync(id);
        var invoice = await GetEditableInvoiceAsync(usage.StayId);

        // Xóa dòng tính phí tương ứng trong Invoice Draft
        // trước khi đánh dấu ServiceUsage bị xóa mềm.
        if (invoice != null)
        {
            var line = await _db.InvoiceLines
                .FirstOrDefaultAsync(x =>
                    x.InvoiceId == invoice.Id &&
                    x.ServiceUsageId == usage.Id);

            if (line != null)
            {
                _db.InvoiceLines.Remove(line);
            }
        }

        // Soft Delete: không xóa ServiceUsage khỏi Database.
        usage.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (invoice != null)
        {
            await RecalculateDraftInvoiceAsync(invoice);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "DeleteServiceUsage",
            EntityType = "ServiceUsage",
            EntityId = usage.Id.ToString(),
            OccurredAt = DateTime.UtcNow,
            Summary =
                $"Xóa mềm dịch vụ ID {usage.Id} " +
                $"của Stay ID {usage.StayId}.",
            OldValuesJson = JsonSerializer.Serialize(new
            {
                usage.Quantity,
                usage.UnitPrice,
                usage.Notes
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                usage.DeletedAt
            })
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }


}

// Developer: NhaNTCE181298
// UC-F09-01: Exception cho lỗi nghiệp vụ.
public class ServiceUsageBusinessException : Exception
{
    public int StatusCode { get; }

    public ServiceUsageBusinessException(
        int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
