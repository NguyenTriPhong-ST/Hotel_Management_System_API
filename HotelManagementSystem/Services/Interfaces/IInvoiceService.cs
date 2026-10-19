using HotelManagementSystem.DTOs.Invoices;

namespace HotelManagementSystem.Services.Interfaces;

// Developer: NhaNTCE181298
// F07 - Invoice Management
public interface IInvoiceService
{
    Task<InvoiceResultDto> CalculateAsync(
        int stayId,
        int accountId);

    Task<InvoiceResultDto> FinalizeAsync(
        int stayId,
        int accountId);
}