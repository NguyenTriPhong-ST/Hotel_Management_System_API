using HotelManagementSystem.DTOs.Stays;

namespace HotelManagementSystem.Services.Interfaces;

// Developer: NhaNTCE181298
// F07 - Stay Management
public interface IStayService
{
    Task<CheckInResponse> CheckInAsync(
        CheckInRequest request,
        int accountId);

    IQueryable<StayListDto> GetStays();

    Task<StayDetailDto?> GetStayDetailAsync(int id);

    Task<CheckOutResponse> CheckOutAsync(
        int stayId,
        int accountId);
}