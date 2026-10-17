using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Data;

/// <summary>
/// Lớp khởi tạo dữ liệu mẫu (Seed Data) cho tất cả các bảng trong CSDL HotelManagementDb.
/// Đảm bảo tính toàn vẹn dữ liệu, các ràng buộc và không tạo bản ghi trùng khi chạy lại nhiều lần (idempotent).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Tự động cập nhật Migration nếu chưa chạy
        await context.Database.MigrateAsync();

        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123", workFactor: 11);

        // ==========================================
        // 1. Seed Accounts (Bảng Accounts)
        // ==========================================
        var desiredAccounts = new List<Account>
        {
            new()
            {
                Username = "admin",
                NormalizedUsername = "ADMIN",
                PasswordHash = defaultPasswordHash,
                FullName = "Quản Trị Viên Hệ Thống",
                PhoneNumber = "0901234567",
                Email = "admin@hotel.com",
                Role = AccountRole.Admin,
                IsActive = true
            },
            new()
            {
                Username = "reception1",
                NormalizedUsername = "RECEPTION1",
                PasswordHash = defaultPasswordHash,
                FullName = "Nguyễn Lễ Tân",
                PhoneNumber = "0912345678",
                Email = "reception1@hotel.com",
                Role = AccountRole.Receptionist,
                IsActive = true
            },
            new()
            {
                Username = "reception2",
                NormalizedUsername = "RECEPTION2",
                PasswordHash = defaultPasswordHash,
                FullName = "Trần Lễ Tân",
                PhoneNumber = "0912345679",
                Email = "reception2@hotel.com",
                Role = AccountRole.Receptionist,
                IsActive = true
            },
            new()
            {
                Username = "staffroom1",
                NormalizedUsername = "STAFFROOM1",
                PasswordHash = defaultPasswordHash,
                FullName = "Lê Dọn Phòng",
                PhoneNumber = "0923456789",
                Email = "staffroom1@hotel.com",
                Role = AccountRole.StaffRoom,
                IsActive = true
            },
            new()
            {
                Username = "staffroom2",
                NormalizedUsername = "STAFFROOM2",
                PasswordHash = defaultPasswordHash,
                FullName = "Phạm Kỹ Thuật",
                PhoneNumber = "0923456780",
                Email = "staffroom2@hotel.com",
                Role = AccountRole.StaffRoom,
                IsActive = true
            },
            new()
            {
                Username = "lockeduser",
                NormalizedUsername = "LOCKEDUSER",
                PasswordHash = defaultPasswordHash,
                FullName = "Tài Khoản Bị Khóa",
                PhoneNumber = "0934567890",
                Email = "locked@hotel.com",
                Role = AccountRole.Receptionist,
                IsActive = false
            }
        };

        foreach (var acc in desiredAccounts)
        {
            var existing = await context.Accounts.FirstOrDefaultAsync(a => a.NormalizedUsername == acc.NormalizedUsername);
            if (existing == null)
            {
                await context.Accounts.AddAsync(acc);
            }
            else
            {
                existing.PasswordHash = acc.PasswordHash;
                existing.Role = acc.Role;
                existing.IsActive = acc.IsActive;
            }
        }
        await context.SaveChangesAsync();

        var adminAcc = await context.Accounts.FirstAsync(a => a.NormalizedUsername == "ADMIN");
        var reception1Acc = await context.Accounts.FirstAsync(a => a.NormalizedUsername == "RECEPTION1");
        var reception2Acc = await context.Accounts.FirstAsync(a => a.NormalizedUsername == "RECEPTION2");
        var staff1Acc = await context.Accounts.FirstAsync(a => a.NormalizedUsername == "STAFFROOM1");
        var staff2Acc = await context.Accounts.FirstAsync(a => a.NormalizedUsername == "STAFFROOM2");

        // ==========================================
        // 2. Seed AuthSessions (Bảng AuthSessions)
        // ==========================================
        if (!await context.AuthSessions.AnyAsync(s => s.AccountId == adminAcc.Id))
        {
            await context.AuthSessions.AddAsync(new AuthSession
            {
                Id = Guid.NewGuid(),
                AccountId = adminAcc.Id,
                IssuedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddHours(23),
                RevokedAt = null
            });
        }
        if (!await context.AuthSessions.AnyAsync(s => s.AccountId == reception1Acc.Id))
        {
            await context.AuthSessions.AddAsync(new AuthSession
            {
                Id = Guid.NewGuid(),
                AccountId = reception1Acc.Id,
                IssuedAt = DateTime.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTime.UtcNow.AddHours(23),
                RevokedAt = null
            });
        }
        if (!await context.AuthSessions.AnyAsync(s => s.AccountId == reception2Acc.Id))
        {
            await context.AuthSessions.AddAsync(new AuthSession
            {
                Id = Guid.NewGuid(),
                AccountId = reception2Acc.Id,
                IssuedAt = DateTime.UtcNow.AddHours(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(19),
                RevokedAt = DateTime.UtcNow.AddHours(-2) // Phiên đã đăng xuất
            });
        }
        await context.SaveChangesAsync();

        // ==========================================
        // 3. Seed RoomTypes (Bảng RoomTypes)
        // ==========================================
        var desiredRoomTypes = new List<RoomType>
        {
            new()
            {
                Code = "STD_SGL",
                Name = "Phòng Tiêu Chuẩn Đơn",
                Description = "Phòng đơn 1 giường 1m4 dành cho 1 người, đầy đủ tiện nghi cơ bản.",
                Capacity = 1,
                BasePricePerNight = 500000m
            },
            new()
            {
                Code = "STD_DBL",
                Name = "Phòng Tiêu Chuẩn Đôi",
                Description = "Phòng đôi 1 giường lớn 1m8 dành cho 2 người, ấm cúng và tiện nghi.",
                Capacity = 2,
                BasePricePerNight = 800000m
            },
            new()
            {
                Code = "DLX_OCN",
                Name = "Phòng Deluxe Hướng Biển",
                Description = "Phòng cao cấp ban công view biển, nội thất sang trọng.",
                Capacity = 2,
                BasePricePerNight = 1500000m
            },
            new()
            {
                Code = "SUI_LUX",
                Name = "Phòng Suite Hoàng Gia",
                Description = "Phòng Suite cao cấp gồm phòng khách riêng, bồn tắm sục jacuzzi.",
                Capacity = 4,
                BasePricePerNight = 3000000m
            }
        };

        foreach (var rt in desiredRoomTypes)
        {
            var existing = await context.RoomTypes.FirstOrDefaultAsync(r => r.Code == rt.Code);
            if (existing == null)
            {
                await context.RoomTypes.AddAsync(rt);
            }
        }
        await context.SaveChangesAsync();

        var stdSgl = await context.RoomTypes.FirstAsync(r => r.Code == "STD_SGL");
        var stdDbl = await context.RoomTypes.FirstAsync(r => r.Code == "STD_DBL");
        var dlxOcn = await context.RoomTypes.FirstAsync(r => r.Code == "DLX_OCN");
        var suiLux = await context.RoomTypes.FirstAsync(r => r.Code == "SUI_LUX");

        // ==========================================
        // 4. Seed Rooms (Bảng Rooms)
        // ==========================================
        var desiredRooms = new List<Room>
        {
            new()
            {
                RoomCode = "P101",
                RoomTypeId = stdSgl.Id,
                FloorNumber = 1,
                Description = "Phòng 101 cạnh thang máy, gần quầy lễ tân.",
                OperationalStatus = RoomOperationalStatus.Active,
                HousekeepingStatus = HousekeepingStatus.Inspected
            },
            new()
            {
                RoomCode = "P102",
                RoomTypeId = stdDbl.Id,
                FloorNumber = 1,
                Description = "Phòng 102 yên tĩnh, cửa sổ nhìn ra vườn hoa.",
                OperationalStatus = RoomOperationalStatus.Active,
                HousekeepingStatus = HousekeepingStatus.Clean
            },
            new()
            {
                RoomCode = "P201",
                RoomTypeId = dlxOcn.Id,
                FloorNumber = 2,
                Description = "Phòng 201 tầng 2 hướng trực diện biển.",
                OperationalStatus = RoomOperationalStatus.Active,
                HousekeepingStatus = HousekeepingStatus.Inspected
            },
            new()
            {
                RoomCode = "P202",
                RoomTypeId = dlxOcn.Id,
                FloorNumber = 2,
                Description = "Phòng 202 tầng 2 view biển, chờ dọn vệ sinh.",
                OperationalStatus = RoomOperationalStatus.Active,
                HousekeepingStatus = HousekeepingStatus.Dirty
            },
            new()
            {
                RoomCode = "P301",
                RoomTypeId = suiLux.Id,
                FloorNumber = 3,
                Description = "Phòng Suite 301 không gian rộng rãi, đang dọn dẹp.",
                OperationalStatus = RoomOperationalStatus.Active,
                HousekeepingStatus = HousekeepingStatus.Cleaning
            },
            new()
            {
                RoomCode = "P302",
                RoomTypeId = suiLux.Id,
                FloorNumber = 3,
                Description = "Phòng Suite 302 tạm khóa bảo trì điều hòa.",
                OperationalStatus = RoomOperationalStatus.OutOfService,
                HousekeepingStatus = HousekeepingStatus.Dirty
            }
        };

        foreach (var rm in desiredRooms)
        {
            var existing = await context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == rm.RoomCode);
            if (existing == null)
            {
                await context.Rooms.AddAsync(rm);
            }
        }
        await context.SaveChangesAsync();

        var room101 = await context.Rooms.FirstAsync(r => r.RoomCode == "P101");
        var room102 = await context.Rooms.FirstAsync(r => r.RoomCode == "P102");
        var room201 = await context.Rooms.FirstAsync(r => r.RoomCode == "P201");
        var room202 = await context.Rooms.FirstAsync(r => r.RoomCode == "P202");
        var room301 = await context.Rooms.FirstAsync(r => r.RoomCode == "P301");
        var room302 = await context.Rooms.FirstAsync(r => r.RoomCode == "P302");

        // ==========================================
        // 5. Seed Guests (Bảng Guests)
        // ==========================================
        var desiredGuests = new List<Guest>
        {
            new()
            {
                FullName = "Nguyễn Văn An",
                DateOfBirth = new DateOnly(1990, 5, 15),
                PhoneNumber = "0981112222",
                Email = "an.nguyen@gmail.com",
                DocumentType = DocumentType.NationalId,
                DocumentNumber = "001090012345",
                DocumentCountryCode = "VNM",
                Address = "123 Cầu Giấy, Hà Nội",
                Notes = "Khách hàng thân thiết"
            },
            new()
            {
                FullName = "Trần Thị Bình",
                DateOfBirth = new DateOnly(1995, 8, 20),
                PhoneNumber = "0982223333",
                Email = "binh.tran@gmail.com",
                DocumentType = DocumentType.NationalId,
                DocumentNumber = "001095054321",
                DocumentCountryCode = "VNM",
                Address = "456 Hải Châu, Đà Nẵng",
                Notes = "Yêu cầu phòng không hút thuốc"
            },
            new()
            {
                FullName = "John Smith",
                DateOfBirth = new DateOnly(1988, 12, 10),
                PhoneNumber = "0983334444",
                Email = "john.smith@foreign.com",
                DocumentType = DocumentType.Passport,
                DocumentNumber = "B12345678",
                DocumentCountryCode = "USA",
                Address = "California, USA",
                Notes = "Giao tiếp tiếng Anh"
            },
            new()
            {
                FullName = "Lê Hoàng Cường",
                DateOfBirth = new DateOnly(2000, 1, 1),
                PhoneNumber = "0984445555",
                Email = "cuong.le@gmail.com",
                DocumentType = DocumentType.NationalId,
                DocumentNumber = "001200098765",
                DocumentCountryCode = "VNM",
                Address = "789 Quận 1, TP. Hồ Chí Minh"
            }
        };

        foreach (var gst in desiredGuests)
        {
            var existing = await context.Guests.FirstOrDefaultAsync(g => g.PhoneNumber == gst.PhoneNumber);
            if (existing == null)
            {
                await context.Guests.AddAsync(gst);
            }
        }
        await context.SaveChangesAsync();

        var guest1 = await context.Guests.FirstAsync(g => g.PhoneNumber == "0981112222");
        var guest2 = await context.Guests.FirstAsync(g => g.PhoneNumber == "0982223333");
        var guest3 = await context.Guests.FirstAsync(g => g.PhoneNumber == "0983334444");
        var guest4 = await context.Guests.FirstAsync(g => g.PhoneNumber == "0984445555");

        // ==========================================
        // 6. Seed Reservations (Bảng Reservations)
        // ==========================================
        var desiredReservations = new List<Reservation>
        {
            new()
            {
                ReservationCode = "RES20261001",
                GuestId = guest1.Id,
                RoomId = room101.Id,
                RoomTypeId = stdSgl.Id,
                CheckInDate = new DateOnly(2026, 10, 10),
                CheckOutDate = new DateOnly(2026, 10, 12),
                NumberOfGuests = 1,
                CapacitySnapshot = 1,
                NightlyRate = 500000m,
                Status = ReservationStatus.Completed,
                Notes = "Đã hoàn thành kỳ lưu trú",
                CreatedByAccountId = adminAcc.Id
            },
            new()
            {
                ReservationCode = "RES20261002",
                GuestId = guest2.Id,
                RoomId = room201.Id,
                RoomTypeId = dlxOcn.Id,
                CheckInDate = new DateOnly(2026, 10, 16),
                CheckOutDate = new DateOnly(2026, 10, 18),
                NumberOfGuests = 2,
                CapacitySnapshot = 2,
                NightlyRate = 1500000m,
                Status = ReservationStatus.CheckedIn,
                Notes = "Khách đang lưu trú tại khách sạn",
                CreatedByAccountId = reception1Acc.Id
            },
            new()
            {
                ReservationCode = "RES20261003",
                GuestId = guest3.Id,
                RoomId = room102.Id,
                RoomTypeId = stdDbl.Id,
                CheckInDate = new DateOnly(2026, 10, 20),
                CheckOutDate = new DateOnly(2026, 10, 23),
                NumberOfGuests = 2,
                CapacitySnapshot = 2,
                NightlyRate = 800000m,
                Status = ReservationStatus.Confirmed,
                Notes = "Đặt trước cho tuần tới",
                CreatedByAccountId = reception1Acc.Id
            },
            new()
            {
                ReservationCode = "RES20261004",
                GuestId = guest4.Id,
                RoomId = room301.Id,
                RoomTypeId = suiLux.Id,
                CheckInDate = new DateOnly(2026, 10, 25),
                CheckOutDate = new DateOnly(2026, 10, 28),
                NumberOfGuests = 3,
                CapacitySnapshot = 4,
                NightlyRate = 3000000m,
                Status = ReservationStatus.Cancelled,
                Notes = "Khách báo bận việc đột xuất",
                CreatedByAccountId = reception2Acc.Id,
                CancelledAt = DateTime.UtcNow.AddDays(-1),
                CancelledByAccountId = adminAcc.Id,
                CancellationReason = "Khách thay đổi kế hoạch công tác"
            }
        };

        foreach (var rsv in desiredReservations)
        {
            var existing = await context.Reservations.FirstOrDefaultAsync(r => r.ReservationCode == rsv.ReservationCode);
            if (existing == null)
            {
                await context.Reservations.AddAsync(rsv);
            }
        }
        await context.SaveChangesAsync();

        var res1 = await context.Reservations.FirstAsync(r => r.ReservationCode == "RES20261001");
        var res2 = await context.Reservations.FirstAsync(r => r.ReservationCode == "RES20261002");

        // ==========================================
        // 7. Seed Stays (Bảng Stays)
        // ==========================================
        var desiredStays = new List<Stay>
        {
            new()
            {
                StayCode = "STY20261001",
                ReservationId = res1.Id,
                RoomId = res1.RoomId,
                CheckedInAt = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc),
                CheckedOutAt = new DateTime(2026, 10, 12, 11, 30, 0, DateTimeKind.Utc),
                CheckedInByAccountId = reception1Acc.Id,
                CheckedOutByAccountId = reception2Acc.Id,
                Notes = "Khách lưu trú 2 đêm hài lòng về dịch vụ"
            },
            new()
            {
                StayCode = "STY20261002",
                ReservationId = res2.Id,
                RoomId = res2.RoomId,
                CheckedInAt = new DateTime(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc),
                CheckedOutAt = null, // Đang lưu trú
                CheckedInByAccountId = reception1Acc.Id,
                Notes = "Khách đi kèm 1 người bạn"
            }
        };

        foreach (var sty in desiredStays)
        {
            var existing = await context.Stays.FirstOrDefaultAsync(s => s.StayCode == sty.StayCode);
            if (existing == null)
            {
                await context.Stays.AddAsync(sty);
            }
        }
        await context.SaveChangesAsync();

        var stay1 = await context.Stays.FirstAsync(s => s.StayCode == "STY20261001");
        var stay2 = await context.Stays.FirstAsync(s => s.StayCode == "STY20261002");

        // ==========================================
        // 8. Seed StayGuests (Bảng StayGuests)
        // ==========================================
        var desiredStayGuests = new List<StayGuest>
        {
            new()
            {
                StayId = stay1.Id,
                GuestId = guest1.Id,
                IsMainGuest = true,
                AddedAt = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                StayId = stay2.Id,
                GuestId = guest2.Id,
                IsMainGuest = true,
                AddedAt = new DateTime(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                StayId = stay2.Id,
                GuestId = guest4.Id,
                IsMainGuest = false, // Khách đi cùng
                AddedAt = new DateTime(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc)
            }
        };

        foreach (var sg in desiredStayGuests)
        {
            var existing = await context.StayGuests.FirstOrDefaultAsync(x => x.StayId == sg.StayId && x.GuestId == sg.GuestId);
            if (existing == null)
            {
                await context.StayGuests.AddAsync(sg);
            }
        }
        await context.SaveChangesAsync();

        // ==========================================
        // 9. Seed HotelServices (Bảng HotelServices)
        // ==========================================
        var desiredServices = new List<HotelService>
        {
            new()
            {
                ServiceCode = "SRV_MINIBAR",
                Name = "Nước suối Aquafina 500ml",
                Description = "Nước khoáng đóng chai tại minibar phòng",
                Unit = "Chai",
                CurrentPrice = 20000m,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV_LAUNDRY",
                Name = "Giặt ủi quần áo tiêu chuẩn",
                Description = "Dịch vụ giặt sấy ủi lấy trong ngày",
                Unit = "Kg",
                CurrentPrice = 45000m,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV_BREAKFAST",
                Name = "Buffet sáng tự chọn",
                Description = "Suất ăn buffet sáng tại nhà hàng tầng 1",
                Unit = "Suất",
                CurrentPrice = 150000m,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV_AIRPORT",
                Name = "Đưa đón sân bay 4 chỗ",
                Description = "Xe riêng đưa đón 1 chiều từ/đến sân bay",
                Unit = "Chuyến",
                CurrentPrice = 350000m,
                IsActive = true
            },
            new()
            {
                ServiceCode = "SRV_OLDSPA",
                Name = "Dịch vụ Massage Cổ truyền (Ngừng KD)",
                Description = "Dịch vụ spa cũ đã dừng khai thác",
                Unit = "Lượt",
                CurrentPrice = 300000m,
                IsActive = false
            }
        };

        foreach (var srv in desiredServices)
        {
            var existing = await context.HotelServices.FirstOrDefaultAsync(s => s.ServiceCode == srv.ServiceCode);
            if (existing == null)
            {
                await context.HotelServices.AddAsync(srv);
            }
        }
        await context.SaveChangesAsync();

        var srvMinibar = await context.HotelServices.FirstAsync(s => s.ServiceCode == "SRV_MINIBAR");
        var srvLaundry = await context.HotelServices.FirstAsync(s => s.ServiceCode == "SRV_LAUNDRY");
        var srvBreakfast = await context.HotelServices.FirstAsync(s => s.ServiceCode == "SRV_BREAKFAST");

        // ==========================================
        // 10. Seed ServiceUsages (Bảng ServiceUsages)
        // ==========================================
        var desiredUsages = new List<ServiceUsage>
        {
            new()
            {
                StayId = stay1.Id,
                HotelServiceId = srvMinibar.Id,
                ServiceNameSnapshot = srvMinibar.Name,
                UnitSnapshot = srvMinibar.Unit,
                Quantity = 2,
                UnitPrice = srvMinibar.CurrentPrice,
                UsedAt = new DateTime(2026, 10, 10, 20, 0, 0, DateTimeKind.Utc),
                RecordedByAccountId = reception1Acc.Id,
                Notes = "Dùng 2 chai nước khoáng"
            },
            new()
            {
                StayId = stay1.Id,
                HotelServiceId = srvLaundry.Id,
                ServiceNameSnapshot = srvLaundry.Name,
                UnitSnapshot = srvLaundry.Unit,
                Quantity = 3,
                UnitPrice = srvLaundry.CurrentPrice,
                UsedAt = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc),
                RecordedByAccountId = reception1Acc.Id,
                Notes = "Giặt 3kg quần áo"
            },
            new()
            {
                StayId = stay2.Id,
                HotelServiceId = srvBreakfast.Id,
                ServiceNameSnapshot = srvBreakfast.Name,
                UnitSnapshot = srvBreakfast.Unit,
                Quantity = 2,
                UnitPrice = srvBreakfast.CurrentPrice,
                UsedAt = new DateTime(2026, 10, 17, 7, 30, 0, DateTimeKind.Utc),
                RecordedByAccountId = reception2Acc.Id,
                Notes = "2 suất ăn sáng buffet"
            }
        };

        foreach (var usg in desiredUsages)
        {
            var existing = await context.ServiceUsages.FirstOrDefaultAsync(u => u.StayId == usg.StayId && u.HotelServiceId == usg.HotelServiceId && u.UsedAt == usg.UsedAt);
            if (existing == null)
            {
                await context.ServiceUsages.AddAsync(usg);
            }
        }
        await context.SaveChangesAsync();

        var usage1 = await context.ServiceUsages.FirstAsync(u => u.StayId == stay1.Id && u.HotelServiceId == srvMinibar.Id);
        var usage2 = await context.ServiceUsages.FirstAsync(u => u.StayId == stay1.Id && u.HotelServiceId == srvLaundry.Id);
        var usage3 = await context.ServiceUsages.FirstAsync(u => u.StayId == stay2.Id && u.HotelServiceId == srvBreakfast.Id);

        // ==========================================
        // 11. Seed Invoices (Bảng Invoices)
        // ==========================================
        var desiredInvoices = new List<Invoice>
        {
            new()
            {
                InvoiceCode = "INV20261001",
                StayId = stay1.Id,
                Status = InvoiceStatus.Finalized,
                BillingFromDate = new DateOnly(2026, 10, 10),
                BillingToDate = new DateOnly(2026, 10, 12),
                GuestNameSnapshot = guest1.FullName,
                GuestDocumentSnapshot = guest1.DocumentNumber,
                RoomCodeSnapshot = room101.RoomCode,
                TotalAmount = 1175000m,
                CreatedByAccountId = reception1Acc.Id,
                FinalizedAt = new DateTime(2026, 10, 12, 11, 30, 0, DateTimeKind.Utc),
                FinalizedByAccountId = reception2Acc.Id,
                Notes = "Hóa đơn hoàn tất thanh toán check-out"
            },
            new()
            {
                InvoiceCode = "INV20261002",
                StayId = stay2.Id,
                Status = InvoiceStatus.Draft,
                BillingFromDate = new DateOnly(2026, 10, 16),
                BillingToDate = new DateOnly(2026, 10, 18),
                GuestNameSnapshot = guest2.FullName,
                GuestDocumentSnapshot = guest2.DocumentNumber,
                RoomCodeSnapshot = room201.RoomCode,
                TotalAmount = 3300000m,
                CreatedByAccountId = reception1Acc.Id,
                Notes = "Hóa đơn tạm tính"
            }
        };

        foreach (var inv in desiredInvoices)
        {
            var existing = await context.Invoices.FirstOrDefaultAsync(i => i.InvoiceCode == inv.InvoiceCode);
            if (existing == null)
            {
                await context.Invoices.AddAsync(inv);
            }
        }
        await context.SaveChangesAsync();

        var inv1Db = await context.Invoices.FirstAsync(i => i.InvoiceCode == "INV20261001");
        var inv2Db = await context.Invoices.FirstAsync(i => i.InvoiceCode == "INV20261002");

        // ==========================================
        // 12. Seed InvoiceLines (Bảng InvoiceLines)
        // ==========================================
        if (!await context.InvoiceLines.AnyAsync(l => l.InvoiceId == inv1Db.Id))
        {
            var lines1 = new List<InvoiceLine>
            {
                new()
                {
                    InvoiceId = inv1Db.Id,
                    LineType = InvoiceLineType.RoomCharge,
                    ServiceUsageId = null,
                    Description = "Tiền phòng P101 (2 đêm)",
                    Unit = "đêm",
                    Quantity = 2,
                    UnitPrice = 500000m
                },
                new()
                {
                    InvoiceId = inv1Db.Id,
                    LineType = InvoiceLineType.ServiceCharge,
                    ServiceUsageId = usage1.Id,
                    Description = usage1.ServiceNameSnapshot,
                    Unit = usage1.UnitSnapshot,
                    Quantity = usage1.Quantity,
                    UnitPrice = usage1.UnitPrice
                },
                new()
                {
                    InvoiceId = inv1Db.Id,
                    LineType = InvoiceLineType.ServiceCharge,
                    ServiceUsageId = usage2.Id,
                    Description = usage2.ServiceNameSnapshot,
                    Unit = usage2.UnitSnapshot,
                    Quantity = usage2.Quantity,
                    UnitPrice = usage2.UnitPrice
                }
            };
            await context.InvoiceLines.AddRangeAsync(lines1);
        }

        if (!await context.InvoiceLines.AnyAsync(l => l.InvoiceId == inv2Db.Id))
        {
            var lines2 = new List<InvoiceLine>
            {
                new()
                {
                    InvoiceId = inv2Db.Id,
                    LineType = InvoiceLineType.RoomCharge,
                    ServiceUsageId = null,
                    Description = "Tiền phòng P201 (2 đêm)",
                    Unit = "đêm",
                    Quantity = 2,
                    UnitPrice = 1500000m
                },
                new()
                {
                    InvoiceId = inv2Db.Id,
                    LineType = InvoiceLineType.ServiceCharge,
                    ServiceUsageId = usage3.Id,
                    Description = usage3.ServiceNameSnapshot,
                    Unit = usage3.UnitSnapshot,
                    Quantity = usage3.Quantity,
                    UnitPrice = usage3.UnitPrice
                }
            };
            await context.InvoiceLines.AddRangeAsync(lines2);
        }
        await context.SaveChangesAsync();

        // ==========================================
        // 13. Seed Payments (Bảng Payments)
        // ==========================================
        var desiredPayments = new List<Payment>
        {
            new()
            {
                ReceiptCode = "REC20261001",
                InvoiceId = inv1Db.Id,
                IdempotencyKey = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                Amount = 1175000m,
                Method = PaymentMethod.Card,
                PaidAt = new DateTime(2026, 10, 12, 11, 35, 0, DateTimeKind.Utc),
                ReferenceNumber = "POS-987654321",
                RecordedByAccountId = reception2Acc.Id,
                CreatedAt = new DateTime(2026, 10, 12, 11, 35, 0, DateTimeKind.Utc),
                Notes = "Thanh toán đầy đủ bằng quẹt thẻ ngân hàng tại quầy"
            }
        };

        foreach (var pay in desiredPayments)
        {
            var existing = await context.Payments.FirstOrDefaultAsync(p => p.ReceiptCode == pay.ReceiptCode);
            if (existing == null)
            {
                await context.Payments.AddAsync(pay);
            }
        }
        await context.SaveChangesAsync();

        // ==========================================
        // 14. Seed HotelTasks (Bảng HotelTasks)
        // ==========================================
        var desiredTasks = new List<HotelTask>
        {
            new()
            {
                TaskCode = "TSK20261001",
                RoomId = room101.Id,
                TaskType = HotelTaskType.Housekeeping,
                HousekeepingKind = HousekeepingKind.Cleaning,
                Title = "Dọn phòng sau khi khách check-out",
                Description = "Thay ga trải giường, vỏ gối, hút bụi, bổ sung bàn chải và khăn tắm.",
                Priority = TaskPriority.Normal,
                Status = HotelTaskStatus.Completed,
                AssignedToAccountId = staff1Acc.Id,
                CreatedByAccountId = reception2Acc.Id,
                StartedAt = new DateTime(2026, 10, 12, 11, 45, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 10, 12, 12, 30, 0, DateTimeKind.Utc),
                CompletedByAccountId = staff1Acc.Id,
                CompletionNotes = "Đã dọn dẹp sạch sẽ và kiểm tra thiết bị hoạt động tốt."
            },
            new()
            {
                TaskCode = "TSK20261002",
                RoomId = room202.Id,
                TaskType = HotelTaskType.Housekeeping,
                HousekeepingKind = HousekeepingKind.Cleaning,
                Title = "Dọn phòng vệ sinh định kỳ",
                Description = "Vệ sinh bồn rửa, lau kính ban công và kiểm tra minibar.",
                Priority = TaskPriority.Normal,
                Status = HotelTaskStatus.Pending,
                AssignedToAccountId = staff1Acc.Id,
                CreatedByAccountId = adminAcc.Id,
                DueAt = DateTime.UtcNow.AddHours(4)
            },
            new()
            {
                TaskCode = "TSK20261003",
                RoomId = room302.Id,
                TaskType = HotelTaskType.Maintenance,
                HousekeepingKind = null, // Maintenance bắt buộc null
                Title = "Sửa máy lạnh rò rỉ nước",
                Description = "Điều hòa phòng 302 chảy nước ra sàn gỗ, cần thông ống thoát nước và kiểm tra gas.",
                Priority = TaskPriority.Urgent,
                Status = HotelTaskStatus.InProgress,
                AssignedToAccountId = staff2Acc.Id,
                CreatedByAccountId = adminAcc.Id,
                StartedAt = DateTime.UtcNow.AddHours(-3),
                DueAt = DateTime.UtcNow.AddHours(2)
            }
        };

        foreach (var tsk in desiredTasks)
        {
            var existing = await context.HotelTasks.FirstOrDefaultAsync(t => t.TaskCode == tsk.TaskCode);
            if (existing == null)
            {
                await context.HotelTasks.AddAsync(tsk);
            }
        }
        await context.SaveChangesAsync();

        var task3 = await context.HotelTasks.FirstAsync(t => t.TaskCode == "TSK20261003");

        // ==========================================
        // 15. Seed RoomBlocks (Bảng RoomBlocks)
        // ==========================================
        var existingBlock = await context.RoomBlocks.FirstOrDefaultAsync(b => b.HotelTaskId == task3.Id);
        if (existingBlock == null)
        {
            var block = new RoomBlock
            {
                RoomId = room302.Id,
                HotelTaskId = task3.Id,
                StartAt = DateTime.UtcNow.AddDays(-1),
                EndAt = DateTime.UtcNow.AddDays(2),
                ReleasedAt = null,
                CancelledAt = null,
                Reason = "Khóa phòng phục vụ sửa chữa và thay thế linh kiện điều hòa",
                CreatedByAccountId = adminAcc.Id
            };

            await context.RoomBlocks.AddAsync(block);
            await context.SaveChangesAsync();
        }

        // ==========================================
        // 16. Seed AuditLogs (Bảng AuditLogs)
        // ==========================================
        if (!await context.AuditLogs.AnyAsync())
        {
            var logs = new List<AuditLog>
            {
                new()
                {
                    ActorAccountId = adminAcc.Id,
                    ActorUsernameSnapshot = adminAcc.Username,
                    Action = "Create",
                    EntityType = "Account",
                    EntityId = adminAcc.Id.ToString(),
                    EntityCodeSnapshot = adminAcc.Username,
                    OccurredAt = DateTime.UtcNow.AddDays(-10),
                    Summary = "Khởi tạo tài khoản quản trị hệ thống ban đầu"
                },
                new()
                {
                    ActorAccountId = reception1Acc.Id,
                    ActorUsernameSnapshot = reception1Acc.Username,
                    Action = "Create",
                    EntityType = "Reservation",
                    EntityId = res2.Id.ToString(),
                    EntityCodeSnapshot = res2.ReservationCode,
                    OccurredAt = DateTime.UtcNow.AddDays(-2),
                    Summary = $"Lễ tân tạo đặt phòng {res2.ReservationCode} cho khách {guest2.FullName}"
                },
                new()
                {
                    ActorAccountId = reception1Acc.Id,
                    ActorUsernameSnapshot = reception1Acc.Username,
                    Action = "CheckIn",
                    EntityType = "Stay",
                    EntityId = stay2.Id.ToString(),
                    EntityCodeSnapshot = stay2.StayCode,
                    OccurredAt = DateTime.UtcNow.AddDays(-1),
                    Summary = $"Lễ tân thực hiện Check-in cho khách {guest2.FullName} vào phòng {room201.RoomCode}"
                }
            };

            await context.AuditLogs.AddRangeAsync(logs);
            await context.SaveChangesAsync();
        }
    }
}
