
using HotelManagementSystem.DTOs.Invoices;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;

namespace HotelManagementSystem.Mappers;

// =====================================================
// Developer: NhaNT
// UC-F07-04: Invoice Mapper
// Chuyển Entity thành DTO kết quả hóa đơn.
// API test: https://localhost:7024/swagger
// =====================================================

public static class InvoiceMapper
{
    // NhaNT | F07-04
    // Chuyển một dòng hóa đơn sang DTO.
    public static InvoiceLineResultDto ToLineDto(
        InvoiceLine line)
    {
        return new InvoiceLineResultDto
        {
            Id = line.Id,
            LineType =
                line.LineType == InvoiceLineType.RoomCharge
                    ? "RoomCharge"
                    : "ServiceCharge",
            ServiceUsageId = line.ServiceUsageId,
            Description = line.Description,
            Unit = line.Unit,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            Amount = line.Amount
        };
    }

    // NhaNT | F07-04
    // Tổng hợp Invoice và các dòng thành DTO trả về.
    public static InvoiceResultDto ToResultDto(
        Invoice invoice,
        string stayCode,
        IEnumerable<InvoiceLine> invoiceLines)
    {
        var lines = invoiceLines
            .Select(ToLineDto)
            .ToList();

        return new InvoiceResultDto
        {
            Id = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            StayId = invoice.StayId,
            StayCode = stayCode,
            Status = invoice.Status == InvoiceStatus.Finalized
                ? "Finalized"
                : "Draft",
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
}
