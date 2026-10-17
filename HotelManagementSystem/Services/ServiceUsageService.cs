
using System.Text.Json;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Interfaces;

namespace HotelManagementSystem.Services;

// =====================================================
// Developer: NhaNTCE181298
// Module: F09 - Service Usage Management
// Layer: Service
//
// Swagger: https://localhost:7024/swagger
// POST   /api/service-usages
// GET    /api/service-usages/{id}
// PUT    /api/service-usages/{id}
// DELETE /api/service-usages/{id}
//
// OData:
// https://localhost:7024/odata/ServiceUsageViews?$top=10
//
// Service xử lý nghiệp vụ qua Repository.
// =====================================================

public class ServiceUsageService
{
    private readonly IServiceUsageRepository _repository;

    public ServiceUsageService(
        IServiceUsageRepository repository)
    {
        _repository = repository;
    }

    // NhaNT | UC-F09-01, 04, 05
    // Chỉ Receptionist đang hoạt động được thao tác.
    private async Task<Account> CheckReceptionistAsync(
        int accountId)
    {
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null || !account.IsActive)
        {
            throw new ServiceUsageBusinessException(
                403,
                "Tài khoản không hợp lệ hoặc đã bị khóa.");
        }

        if (account.Role != AccountRole.Receptionist)
        {
            throw new ServiceUsageBusinessException(
                403,
                "Tài khoản không có quyền quản lý dịch vụ.");
        }

        return account;
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Kiểm tra lượt sử dụng chưa xóa và Stay chưa check-out.
    private async Task<ServiceUsage> GetEditableUsageAsync(
        int id)
    {
        var usage =
            await _repository.GetEditableUsageAsync(id);

        if (usage == null || usage.DeletedAt != null)
        {
            throw new ServiceUsageBusinessException(
                404,
                "Không tìm thấy lượt sử dụng dịch vụ.");
        }

        if (usage.Stay.CheckedOutAt != null)
        {
            throw new ServiceUsageBusinessException(
                409,
                "Stay đã check-out, không thể chỉnh sửa dịch vụ.");
        }

        return usage;
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Invoice Finalized không được sửa chi phí dịch vụ.
    private async Task<Invoice?> GetEditableInvoiceAsync(
        int stayId)
    {
        var invoice =
            await _repository.GetInvoiceAsync(stayId);

        if (invoice != null &&
            invoice.Status == InvoiceStatus.Finalized)
        {
            throw new ServiceUsageBusinessException(
                409,
                "Hóa đơn đã chốt, không thể chỉnh sửa dịch vụ.");
        }

        return invoice;
    }

    // NhaNT | UC-F09-04, UC-F09-05
    // Tính lại tổng hóa đơn sau khi SQL tính InvoiceLine.Amount.
    private async Task RecalculateDraftInvoiceAsync(
        Invoice invoice)
    {
        invoice.TotalAmount =
            await _repository.GetInvoiceTotalAsync(invoice.Id);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-01: Create Service Usage
    //
    // Swagger: https://localhost:7024/swagger
    // Method: POST
    // Endpoint: /api/service-usages
    // Header: X-Demo-Account-Id
    // Body: CreateServiceUsageRequest
    //
    // Kết quả: 201 - Ghi nhận dịch vụ thành công.
    // =================================================
    public async Task<CreateServiceUsageResponse> CreateAsync(
        CreateServiceUsageRequest request,
        int accountId)
    {
        var now = DateTime.UtcNow;

        await using var transaction =
            await _repository.BeginTransactionAsync();

        // NhaNT | F09-01 - Kiểm tra Receptionist.
        var account =
            await _repository.GetAccountAsync(accountId);

        if (account == null || !account.IsActive)
        {
            throw new ServiceUsageBusinessException(
                403,
                "Tài khoản không hợp lệ hoặc đã bị khóa.");
        }

        if (account.Role != AccountRole.Receptionist)
        {
            throw new ServiceUsageBusinessException(
                403,
                "Tài khoản không có quyền ghi nhận dịch vụ.");
        }

        // NhaNT | F09-01 - Kiểm tra Stay còn hoạt động.
        var stay = await _repository.GetStayAsync(
            request.StayId);

        if (stay == null)
        {
            throw new ServiceUsageBusinessException(
                404, "Không tìm thấy Stay.");
        }

        if (stay.CheckedOutAt != null)
        {
            throw new ServiceUsageBusinessException(
                409,
                "Stay đã check-out, không thể thêm dịch vụ.");
        }

        // NhaNT | F09-01 - Không thêm sau khi chốt hóa đơn.
        if (await _repository.HasFinalizedInvoiceAsync(
                request.StayId))
        {
            throw new ServiceUsageBusinessException(
                409,
                "Hóa đơn đã chốt, không thể thêm dịch vụ.");
        }

        // NhaNT | F09-01 - Kiểm tra dịch vụ đang hoạt động.
        var hotelService =
            await _repository.GetHotelServiceAsync(
                request.HotelServiceId);

        if (hotelService == null)
        {
            throw new ServiceUsageBusinessException(
                404, "Không tìm thấy dịch vụ.");
        }

        if (!hotelService.IsActive)
        {
            throw new ServiceUsageBusinessException(
                409,
                "Dịch vụ hiện không hoạt động.");
        }

        if (request.Quantity <= 0)
        {
            throw new ServiceUsageBusinessException(
                400,
                "Số lượng sử dụng phải lớn hơn 0.");
        }

        // NhaNT | F09-01 - Snapshot tên, đơn vị và giá.
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

        _repository.AddUsage(usage);
        await _repository.SaveChangesAsync();

        // NhaNT | F09-01 - AuditLog trong transaction.
        _repository.AddAuditLog(new AuditLog
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

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceUsageMapper.ToResponse(
            usage,
            "Ghi nhận dịch vụ thành công.");
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-02: View Service Usages
    //
    // ODATA - COPY LINK TEST:
    // https://localhost:7024/odata/ServiceUsageViews?$top=10
    // https://localhost:7024/odata/ServiceUsageViews?$filter=StayId%20eq%201
    // https://localhost:7024/odata/ServiceUsageViews?$orderby=Id%20desc
    // https://localhost:7024/odata/ServiceUsageViews?$skip=0&$top=10&$count=true
    // =================================================
    public IQueryable<ServiceUsageListDto> GetServiceUsages()
    {
        return _repository.QueryUsageViews();
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-03: View Service Usage Details
    //
    // Swagger: https://localhost:7024/swagger
    // Method: GET
    // Endpoint: /api/service-usages/{id}
    // Input: id = ServiceUsage ID.
    // =================================================
    public Task<ServiceUsageDetailDto?> GetDetailAsync(int id)
    {
        return _repository.GetDetailAsync(id);
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-04: Update Service Usage
    //
    // Swagger: https://localhost:7024/swagger
    // Method: PUT
    // Endpoint: /api/service-usages/{id}
    // Header: X-Demo-Account-Id
    // Body: UpdateServiceUsageRequest
    //
    // Chỉ cập nhật Quantity và Notes.
    // =================================================
    public async Task<CreateServiceUsageResponse> UpdateAsync(
        int id,
        UpdateServiceUsageRequest request,
        int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        var account =
            await CheckReceptionistAsync(accountId);

        var usage =
            await GetEditableUsageAsync(id);

        var invoice =
            await GetEditableInvoiceAsync(usage.StayId);

        if (request.Quantity <= 0)
        {
            throw new ServiceUsageBusinessException(
                400, "Quantity phải lớn hơn 0.");
        }

        var oldQuantity = usage.Quantity;
        var oldNotes = usage.Notes;

        // NhaNT | F09-04 - Không thay đổi giá snapshot.
        usage.Quantity = request.Quantity;
        usage.Notes = request.Notes?.Trim();

        if (invoice != null)
        {
            var line =
                await _repository.GetInvoiceLineAsync(
                    invoice.Id,
                    usage.Id);

            if (line == null)
            {
                // NhaNT | F09-04
                // Bổ sung dòng dịch vụ thiếu trong Draft.
                line = new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    ServiceUsageId = usage.Id,
                    LineType = InvoiceLineType.ServiceCharge,
                    Description = usage.ServiceNameSnapshot,
                    Unit = usage.UnitSnapshot,
                    Quantity = usage.Quantity,
                    UnitPrice = usage.UnitPrice
                };

                _repository.AddInvoiceLine(line);
            }
            else
            {
                // NhaNT | F09-04
                // Đồng bộ số lượng và thông tin snapshot.
                line.Quantity = usage.Quantity;
                line.UnitPrice = usage.UnitPrice;
                line.Description = usage.ServiceNameSnapshot;
                line.Unit = usage.UnitSnapshot;
            }
        }

        // NhaNT | F09-04
        // Lưu trước để SQL Server cập nhật Amount.
        await _repository.SaveChangesAsync();

        if (invoice != null)
        {
            await RecalculateDraftInvoiceAsync(invoice);
        }

        // NhaNT | F09-04 - Lưu AuditLog.
        _repository.AddAuditLog(new AuditLog
        {
            ActorAccountId = account.Id,
            ActorUsernameSnapshot = account.Username,
            Action = "UpdateServiceUsage",
            EntityType = "ServiceUsage",
            EntityId = usage.Id.ToString(),
            OccurredAt = DateTime.UtcNow,
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

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceUsageMapper.ToResponse(
            usage,
            "Cập nhật lượt sử dụng dịch vụ thành công.");
    }

    // =================================================
    // Developer: NhaNTCE181298
    // UC-F09-05: Soft Delete Service Usage
    //
    // Swagger: https://localhost:7024/swagger
    // Method: DELETE
    // Endpoint: /api/service-usages/{id}
    // Header: X-Demo-Account-Id
    //
    // Xóa mềm ServiceUsage và xóa phí khỏi Invoice Draft.
    // =================================================
    public async Task DeleteAsync(int id, int accountId)
    {
        await using var transaction =
            await _repository.BeginTransactionAsync();

        var account =
            await CheckReceptionistAsync(accountId);

        var usage =
            await GetEditableUsageAsync(id);

        var invoice =
            await GetEditableInvoiceAsync(usage.StayId);

        // NhaNT | F09-05 - Xóa dòng phí trong Draft.
        if (invoice != null)
        {
            var line =
                await _repository.GetInvoiceLineAsync(
                    invoice.Id,
                    usage.Id);

            if (line != null)
            {
                _repository.RemoveInvoiceLine(line);
            }
        }

        // NhaNT | F09-05
        // Soft delete, giữ ServiceUsage trong Database.
        usage.DeletedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        if (invoice != null)
        {
            await RecalculateDraftInvoiceAsync(invoice);
        }

        // NhaNT | F09-05 - Ghi AuditLog.
        _repository.AddAuditLog(new AuditLog
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

        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}

// =====================================================
// Developer: NhaNTCE181298
// UC-F09-01 đến UC-F09-05
// Exception trả lỗi nghiệp vụ qua Controller.
// =====================================================
public class ServiceUsageBusinessException : Exception
{
    public int StatusCode { get; }

    public ServiceUsageBusinessException(
        int statusCode,
        string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
