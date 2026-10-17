using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Data;

/// <summary>
/// Khởi tạo và nạp dữ liệu mẫu (Data Seeding) tự động cho hệ thống khi ứng dụng khởi chạy.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Xóa toàn bộ dữ liệu test cũ của Guests và HotelServices, sau đó nạp lại bộ dữ liệu mẫu chuẩn từ đầu.
    /// </summary>
    public static async Task ResetAndSeedAsync(AppDbContext context)
    {
        // 1. Dọn sạch dữ liệu cũ và reset ID tự tăng (nếu không có ràng buộc khóa ngoại chặn)
        var hasActiveStayGuests = await context.StayGuests.AnyAsync();
        var hasActiveUsages = await context.ServiceUsages.AnyAsync();
        var hasReservations = await context.Reservations.AnyAsync();

        if (!hasActiveStayGuests && !hasActiveUsages && !hasReservations)
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [Guests]; DBCC CHECKIDENT ('Guests', RESEED, 0);");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM [HotelServices]; DBCC CHECKIDENT ('HotelServices', RESEED, 0);");
        }

        // 2. Nạp dữ liệu mẫu
        await SeedAsync(context);
    }

    /// <summary>
    /// Nạp dữ liệu mẫu theo cơ chế Idempotent (chống trùng lặp khi chạy lại nhiều lần).
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedGuestsAsync(context);
        await SeedHotelServicesAsync(context);
    }

    private static async Task SeedGuestsAsync(AppDbContext context)
    {
        var sampleGuests = new List<Guest>
        {
            new()
            {
                FullName = "Nguyễn Văn An",
                DateOfBirth = new DateOnly(1990, 5, 15),
                PhoneNumber = "0901234567",
                Email = "an.nguyen@example.com",
                DocumentType = DocumentType.NationalId,
                DocumentNumber = "079090001111",
                DocumentCountryCode = "VNM",
                Address = "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                Notes = "Khách VIP thân thiết, thích phòng tầng cao, view thoáng mát."
            },
            new()
            {
                FullName = "Trần Thị Mai",
                DateOfBirth = new DateOnly(1995, 10, 20),
                PhoneNumber = "0987654321",
                Email = "mai.tran@example.com",
                DocumentType = DocumentType.NationalId,
                DocumentNumber = "079195002222",
                DocumentCountryCode = "VNM",
                Address = "456 Nguyễn Thị Minh Khai, Phường 5, Quận 3, TP. Hồ Chí Minh",
                Notes = "Khách ăn chay, yêu cầu chuẩn bị đồ dùng phòng không có lông vũ."
            },
            new()
            {
                FullName = "Lê Hoàng Phúc",
                DateOfBirth = new DateOnly(1988, 3, 25),
                PhoneNumber = "0912345678",
                Email = "phuc.le@example.com",
                DocumentType = DocumentType.Passport,
                DocumentNumber = "C8889999",
                DocumentCountryCode = "VNM",
                Address = "789 Điện Biên Phủ, Quận Hải Châu, TP. Đà Nẵng",
                Notes = "Khách công tác thường xuyên, hóa đơn thanh toán qua công ty."
            },
            new()
            {
                FullName = "David Smith",
                DateOfBirth = new DateOnly(1985, 7, 12),
                PhoneNumber = "+14155552671",
                Email = "david.smith@example.com",
                DocumentType = DocumentType.Passport,
                DocumentNumber = "A12345678",
                DocumentCountryCode = "USA",
                Address = "San Francisco, California, USA",
                Notes = "Khách du lịch quốc tế, giao tiếp tiếng Anh."
            },
            new()
            {
                FullName = "Emily Watson",
                DateOfBirth = new DateOnly(1992, 11, 4),
                PhoneNumber = "+447911123456",
                Email = "emily.watson@example.co.uk",
                DocumentType = DocumentType.Passport,
                DocumentNumber = "GB98765432",
                DocumentCountryCode = "GBR",
                Address = "London, United Kingdom",
                Notes = "Khách yêu thích tour trải nghiệm địa phương và ẩm thực đường phố."
            },
            new()
            {
                FullName = "Phạm Quỳnh Như",
                DateOfBirth = new DateOnly(1998, 9, 30),
                PhoneNumber = "0909888777",
                Email = "quynhnhu.pham@example.com",
                DocumentType = null,
                DocumentNumber = null,
                DocumentCountryCode = null,
                Address = "Quận Ninh Kiều, TP. Cần Thơ",
                Notes = "Khách đặt trước qua hotline, sẽ bổ sung CCCD khi làm thủ tục nhận phòng."
            }
        };

        foreach (var guest in sampleGuests)
        {
            // Kiểm tra chống trùng: Nếu có giấy tờ thì kiểm tra theo bộ 3 giấy tờ; nếu chưa thì kiểm tra theo SĐT
            bool exists;
            if (!string.IsNullOrWhiteSpace(guest.DocumentNumber) && guest.DocumentType.HasValue)
            {
                exists = await context.Guests.AnyAsync(g =>
                    g.DeletedAt == null &&
                    g.DocumentType == guest.DocumentType &&
                    g.DocumentCountryCode == guest.DocumentCountryCode &&
                    g.DocumentNumber == guest.DocumentNumber);
            }
            else
            {
                exists = await context.Guests.AnyAsync(g =>
                    g.DeletedAt == null &&
                    g.PhoneNumber == guest.PhoneNumber &&
                    g.FullName == guest.FullName);
            }

            if (!exists)
            {
                context.Guests.Add(guest);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedHotelServicesAsync(AppDbContext context)
    {
        var sampleServices = new List<HotelService>
        {
            new()
            {
                ServiceCode = "SRV-LAUNDRY-STD",
                Name = "Giặt ủi quần áo tiêu chuẩn",
                Description = "Giặt sấy và ủi phẳng quần áo thông thường trong vòng 24 giờ.",
                Unit = "Bộ",
                CurrentPrice = 50000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-LAUNDRY-EXP",
                Name = "Giặt ủi lấy nhanh (Express)",
                Description = "Giặt hấp và ủi cao cấp lấy gấp trong vòng 3 giờ.",
                Unit = "Bộ",
                CurrentPrice = 90000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-AIRPORT-4S",
                Name = "Xe đưa đón sân bay 4 chỗ",
                Description = "Xe sedan 4 chỗ đời mới, phục vụ đưa đón 1 chiều từ/đến sân bay.",
                Unit = "Lượt",
                CurrentPrice = 250000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-AIRPORT-7S",
                Name = "Xe đưa đón sân bay 7 chỗ VIP",
                Description = "Xe SUV/MPV 7 chỗ sang trọng đưa đón sân bay kèm nước uống và khăn lạnh.",
                Unit = "Lượt",
                CurrentPrice = 400000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-MINIBAR-COKE",
                Name = "Coca-Cola Minibar 330ml",
                Description = "Nước ngọt có gas ướp lạnh sẵn trong tủ lạnh mini tại phòng.",
                Unit = "Lon",
                CurrentPrice = 25000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-MINIBAR-WATER",
                Name = "Nước khoáng Lavie 500ml",
                Description = "Nước khoáng thiên nhiên đóng chai (ngoài phần miễn phí hàng ngày).",
                Unit = "Chai",
                CurrentPrice = 15000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-MINIBAR-BEER",
                Name = "Bia Heineken lon 330ml",
                Description = "Bia cao cấp ướp lạnh trong minibar phòng.",
                Unit = "Lon",
                CurrentPrice = 40000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-BUFFET-BKF",
                Name = "Buffet sáng quốc tế",
                Description = "Thưởng thức bữa sáng tự chọn đa dạng món ăn Á - Âu tại nhà hàng tầng 2.",
                Unit = "Suất",
                CurrentPrice = 180000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-SPA-MASSAGE",
                Name = "Massage thư giãn toàn thân 60 phút",
                Description = "Liệu trình massage tinh dầu thảo dược thiên nhiên phục hồi năng lượng tại Spa.",
                Unit = "Suất",
                CurrentPrice = 450000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-EXTRA-BED",
                Name = "Giường phụ kê thêm (Extra Bed)",
                Description = "Kê thêm giường đơn 1.2m kèm bộ chăn drap gối lông vũ cao cấp.",
                Unit = "Đêm",
                CurrentPrice = 300000,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV-TOUR-CITY",
                Name = "Tour tham quan thành phố nửa ngày",
                Description = "Tour ngắm cảnh các địa danh nổi tiếng có hướng dẫn viên đi kèm.",
                Unit = "Khách",
                CurrentPrice = 350000,
                IsActive = false // Tạm ngừng cung cấp mùa mưa
            }
        };

        foreach (var service in sampleServices)
        {
            var exists = await context.HotelServices.AnyAsync(s => s.ServiceCode == service.ServiceCode);
            if (!exists)
            {
                context.HotelServices.Add(service);
            }
        }

        await context.SaveChangesAsync();
    }
}
