using HotelManagementSystem.Data;
using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Data.Seed;

// ============================================================================
// Developer: NhaNTCE181298
// DB-03 (du lieu mau ho tro): F07-01..07, F09-01..06, F11-01..04
// Mo ta: Tao du lieu Development de kiem thu API/Swagger, khong dung cho Production.
// OData NOTE: Seeder ghi du lieu bang EF Core; OData chi su dung cho API GET sau nay.
// ============================================================================
public static class DevelopmentDataSeeder
{
    private const string DemoPassword = "Demo@123456";

    public static async Task SeedAsync(AppDbContext db)
    {
        // NhaNTCE181298 | DB-03 | Khong seed trung va khong chen vao DB da co du lieu.
        if (await db.Accounts.AnyAsync(a => a.Username == "demo.admin"))
        {
            Console.WriteLine("[DEMO SEED] Du lieu mau da ton tai. Bo qua.");
            return;
        }

        if (await db.Accounts.AnyAsync() || await db.RoomTypes.AnyAsync() ||
            await db.Rooms.AnyAsync() || await db.Guests.AnyAsync() ||
            await db.Reservations.AnyAsync() || await db.Stays.AnyAsync() ||
            await db.HotelServices.AnyAsync() || await db.HotelTasks.AnyAsync() ||
            await db.Invoices.AnyAsync() || await db.Payments.AnyAsync())
        {
            throw new InvalidOperationException(
                "Database da co du lieu khac. Khong tu dong them demo de tranh trung/xung dot.");
        }

        // NhaNTCE181298 | F07/F09/F11 | Tao tat ca bang mot transaction: loi thi rollback.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var now = DateTime.UtcNow;

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F07-01, F09-01, F11-04 | Nhan vien: 1 Admin, 2 le tan,
        // 2 nhan vien buong phong. Password băm bang ASP.NET Identity PasswordHasher.
        // --------------------------------------------------------------------
        var hasher = new PasswordHasher<Account>();
        Account CreateAccount(string username, string fullName, AccountRole role)
        {
            var account = new Account
            {
                Username = username,
                NormalizedUsername = username.Trim().ToUpperInvariant(),
                FullName = fullName,
                Role = role,
                IsActive = true,
                Email = $"{username}@hotel-demo.test"
            };
            account.PasswordHash = hasher.HashPassword(account, DemoPassword);
            return account;
        }

        var admin = CreateAccount("demo.admin", "Quan tri vien Demo", AccountRole.Admin);
        var receptionists = new[]
        {
            CreateAccount("demo.reception1", "Le tan Minh Anh", AccountRole.Receptionist),
            CreateAccount("demo.reception2", "Le tan Hoang Nam", AccountRole.Receptionist)
        };
        var roomStaff = new[]
        {
            CreateAccount("demo.room1", "Nhan vien buong Lan", AccountRole.StaffRoom),
            CreateAccount("demo.room2", "Nhan vien buong Phuc", AccountRole.StaffRoom)
        };
        db.Accounts.AddRange(new[] { admin }.Concat(receptionists).Concat(roomStaff));

        // NhaNTCE181298 | F11-01/F07-01 | 4 loai phong co gia va suc chua khac nhau.
        var types = new[]
        {
            new RoomType { Code = "STD", Name = "Standard", Capacity = 2, BasePricePerNight = 500000m },
            new RoomType { Code = "DLX", Name = "Deluxe", Capacity = 3, BasePricePerNight = 800000m },
            new RoomType { Code = "FAM", Name = "Family", Capacity = 4, BasePricePerNight = 1200000m },
            new RoomType { Code = "STE", Name = "Suite", Capacity = 2, BasePricePerNight = 2000000m }
        };
        db.RoomTypes.AddRange(types);

        // NhaNTCE181298 | F07-01/F11-02 | Ho so khach la du lieu gia lap, khong co tai khoan dang nhap.
        var guestNames = new[]
        {
            "Nguyen Minh Khang", "Tran Ngoc Han", "Le Gia Bao", "Pham Thu Ha",
            "Vo Quoc Huy", "Dang My Linh", "Bui Thanh Tung", "Hoang Minh Chau",
            "Do Anh Khoa", "Huynh Thuy Tien", "Nguyen Hoai Nam", "Tran Bao Ngan",
            "Le Tuan Kiet", "Pham Ngoc Anh", "Vo Thanh Phong", "Dang Ngoc Mai",
            "Bui Gia Huy", "Hoang Nhat Linh", "Do Minh Tri", "Huynh Bao Tram",
            "Nguyen Tien Dat", "Tran Kim Ngan", "Le Huu Phuc", "Pham Thanh Thao",
            "Vo Duc Minh", "Dang Ngoc Quynh"
        };
        var guests = guestNames.Select((name, i) => new Guest
        {
            FullName = name,
            PhoneNumber = $"09{(10000000 + i).ToString("D8")}",
            Email = $"guest{i + 1:00}@example.test",
            Notes = "Ho so mau phuc vu kiem thu API"
        }).ToArray();
        db.Guests.AddRange(guests);

        // NhaNTCE181298 | F09-01/F11-03 | Danh muc dich vu va don gia hien tai.
        var hotelServices = new[]
        {
            new HotelService { ServiceCode = "SV001", Name = "An sang", Unit = "suat", CurrentPrice = 90000m, IsActive = true },
            new HotelService { ServiceCode = "SV002", Name = "Giat ui", Unit = "kg", CurrentPrice = 55000m, IsActive = true },
            new HotelService { ServiceCode = "SV003", Name = "Minibar", Unit = "phan", CurrentPrice = 70000m, IsActive = true },
            new HotelService { ServiceCode = "SV004", Name = "Spa", Unit = "luot", CurrentPrice = 450000m, IsActive = true },
            new HotelService { ServiceCode = "SV005", Name = "Dua don san bay", Unit = "chuyen", CurrentPrice = 350000m, IsActive = true },
            new HotelService { ServiceCode = "SV006", Name = "Thue xe dap", Unit = "gio", CurrentPrice = 40000m, IsActive = true },
            new HotelService { ServiceCode = "SV007", Name = "In tai lieu", Unit = "trang", CurrentPrice = 5000m, IsActive = true },
            new HotelService { ServiceCode = "SV008", Name = "Tra phong muon", Unit = "gio", CurrentPrice = 150000m, IsActive = true },
            new HotelService { ServiceCode = "SV009", Name = "Phong hop", Unit = "gio", CurrentPrice = 300000m, IsActive = false }
        };
        db.HotelServices.AddRange(hotelServices);
        await db.SaveChangesAsync();

        // NhaNTCE181298 | F07-01/F11-01 | 16 phong: sach, ban, dang don va tam ngung hoat dong.
        var typeByCode = types.ToDictionary(t => t.Code);
        var roomSpecs = new (string Code, string Type, short Floor,
            RoomOperationalStatus Operation, HousekeepingStatus Cleaning)[]
        {
            ("P101", "STD", 1, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P102", "STD", 1, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P103", "STD", 1, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P104", "STD", 1, RoomOperationalStatus.Active, HousekeepingStatus.Dirty),
            ("P201", "DLX", 2, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P202", "DLX", 2, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P203", "DLX", 2, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P204", "DLX", 2, RoomOperationalStatus.Active, HousekeepingStatus.Cleaning),
            ("P301", "FAM", 3, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P302", "FAM", 3, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P303", "FAM", 3, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P304", "FAM", 3, RoomOperationalStatus.OutOfService, HousekeepingStatus.Dirty),
            ("P401", "STE", 4, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P402", "STE", 4, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P403", "STE", 4, RoomOperationalStatus.Active, HousekeepingStatus.Inspected),
            ("P404", "STE", 4, RoomOperationalStatus.Active, HousekeepingStatus.Clean)
        };
        var rooms = roomSpecs.Select(s => new Room
        {
            RoomCode = s.Code,
            RoomTypeId = typeByCode[s.Type].Id,
            FloorNumber = s.Floor,
            OperationalStatus = s.Operation,
            HousekeepingStatus = s.Cleaning,
            Description = $"Phong {s.Code} - {s.Type}"
        }).ToArray();
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
        var roomByCode = rooms.ToDictionary(r => r.RoomCode);

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F07-01..07, F11-02 | 36 Reservations:
        // 12 Completed, 7 CheckedIn, 10 Confirmed, 4 Cancelled, 3 NoShow.
        // --------------------------------------------------------------------
        var completed = new List<Reservation>();
        var active = new List<Reservation>();
        var confirmed = new List<Reservation>();
        var reservationCounter = 0;

        Reservation AddReservation(string code, string roomCode, DateOnly arrival,
            int nights, ReservationStatus status)
        {
            var room = roomByCode[roomCode];
            var roomType = types.Single(t => t.Id == room.RoomTypeId);
            var index = reservationCounter++;
            var reservation = new Reservation
            {
                ReservationCode = code,
                GuestId = guests[(index * 2) % guests.Length].Id,
                RoomId = room.Id,
                RoomTypeId = roomType.Id,
                CheckInDate = arrival,
                CheckOutDate = arrival.AddDays(nights),
                NumberOfGuests = Math.Min(roomType.Capacity, index % 3 == 0 ? 3 : 2),
                CapacitySnapshot = roomType.Capacity,
                NightlyRate = roomType.BasePricePerNight,
                Status = status,
                CreatedByAccountId = receptionists[index % 2].Id,
                Notes = $"Du lieu mau {status} - {roomCode}"
            };
            if (status == ReservationStatus.Cancelled)
            {
                reservation.CancelledAt = now.AddDays(-2);
                reservation.CancelledByAccountId = receptionists[index % 2].Id;
                reservation.CancellationReason = "Khach huy dat phong (demo)";
            }
            db.Reservations.Add(reservation);
            return reservation;
        }

        for (var i = 0; i < 12; i++)
            completed.Add(AddReservation($"RS-C{i + 1:000}", rooms[i].RoomCode,
                today.AddDays(-70 + i * 4), 2, ReservationStatus.Completed));

        var activeRoomCodes = new[] { "P101", "P102", "P201", "P202", "P301", "P302", "P401" };
        for (var i = 0; i < activeRoomCodes.Length; i++)
            active.Add(AddReservation($"RS-A{i + 1:000}", activeRoomCodes[i],
                today.AddDays(-1 - i % 3), 5 + i % 3, ReservationStatus.CheckedIn));

        var futureRoomCodes = new[] { "P103", "P104", "P203", "P204", "P303",
            "P402", "P403", "P404", "P101", "P301" };
        for (var i = 0; i < futureRoomCodes.Length; i++)
            confirmed.Add(AddReservation($"RS-F{i + 1:000}", futureRoomCodes[i],
                today.AddDays(i < 8 ? 3 + i : 15 + i - 8), 2,
                ReservationStatus.Confirmed));

        for (var i = 0; i < 4; i++)
            AddReservation($"RS-X{i + 1:000}", rooms[(i + 4) % rooms.Length].RoomCode,
                today.AddDays(1 + i), 2, ReservationStatus.Cancelled);

        for (var i = 0; i < 3; i++)
            AddReservation($"RS-N{i + 1:000}", rooms[(i + 10) % rooms.Length].RoomCode,
                today.AddDays(-12 - i), 2, ReservationStatus.NoShow);

        await db.SaveChangesAsync();

        // NhaNTCE181298 | F11-02 | Dieu chinh CreatedAt lich su cho bao cao
        // theo ngay tao reservation (SaveChanges tu dong gan CreatedAt = hien tai khi Add).
        foreach (var reservation in db.Reservations.Local)
        {
            var createdDate = reservation.Status switch
            {
                ReservationStatus.Completed => reservation.CheckInDate.AddDays(-3),
                ReservationStatus.CheckedIn => reservation.CheckInDate.AddDays(-5),
                ReservationStatus.Confirmed => today.AddDays(-3),
                ReservationStatus.Cancelled => today.AddDays(-7),
                ReservationStatus.NoShow => reservation.CheckInDate.AddDays(-3),
                _ => today
            };
            reservation.CreatedAt = AtVietnamTime(createdDate, 10);
            db.Entry(reservation).Property(r => r.CreatedAt).IsModified = true;
        }
        await db.SaveChangesAsync();

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F07-01/02/03 | 19 Stays va StayGuests.
        // MainGuest luon trung voi GuestId cua Reservation va khong vuot CapacitySnapshot.
        // --------------------------------------------------------------------
        var allStayReservations = completed.Concat(active).ToArray();
        var stays = new List<Stay>();
        for (var i = 0; i < allStayReservations.Length; i++)
        {
            var reservation = allStayReservations[i];
            var isCompleted = i < completed.Count;
            var stay = new Stay
            {
                StayCode = $"ST-{i + 1:000}",
                ReservationId = reservation.Id,
                RoomId = reservation.RoomId,
                CheckedInAt = AtVietnamTime(reservation.CheckInDate, 14),
                CheckedOutAt = isCompleted ? AtVietnamTime(reservation.CheckOutDate, 11) : null,
                CheckedInByAccountId = receptionists[i % 2].Id,
                CheckedOutByAccountId = isCompleted ? receptionists[(i + 1) % 2].Id : null,
                Notes = isCompleted ? "Da tra phong - demo" : "Dang luu tru - demo"
            };
            db.Stays.Add(stay);
            stays.Add(stay);

            stay.StayGuests.Add(new StayGuest
            {
                GuestId = reservation.GuestId,
                IsMainGuest = true,
                AddedAt = stay.CheckedInAt
            });
            var mainGuestIndex = Array.FindIndex(guests, g => g.Id == reservation.GuestId);
            for (var j = 1; j < reservation.NumberOfGuests; j++)
            {
                stay.StayGuests.Add(new StayGuest
                {
                    GuestId = guests[(mainGuestIndex + j) % guests.Length].Id,
                    IsMainGuest = false,
                    AddedAt = stay.CheckedInAt
                });
            }
        }
        await db.SaveChangesAsync();

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F09-01..06 | 3 luot dich vu / Stay, 2 luot xoa mem.
        // Luon giu UnitPrice, ServiceNameSnapshot va UnitSnapshot de test gia lich su.
        // --------------------------------------------------------------------
        var usagesByStay = new Dictionary<int, List<ServiceUsage>>();
        for (var i = 0; i < stays.Count; i++)
        {
            var stay = stays[i];
            var usages = new List<ServiceUsage>();
            for (var j = 0; j < 3; j++)
            {
                var svc = hotelServices[(i * 2 + j) % 8]; // Chi dung dich vu dang cung cap.
                var quantity = j == 2 ? 1.5m : j + 1;
                var unitPrice = i < completed.Count && j == 0
                    ? svc.CurrentPrice - 5000m : svc.CurrentPrice;
                var usage = new ServiceUsage
                {
                    StayId = stay.Id,
                    HotelServiceId = svc.Id,
                    ServiceNameSnapshot = svc.Name,
                    UnitSnapshot = svc.Unit,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    UsedAt = stay.CheckedInAt.AddHours(3 + j * 2),
                    RecordedByAccountId = receptionists[j % 2].Id,
                    Notes = "Su dung dich vu mau"
                };
                if (i >= completed.Count && i < completed.Count + 2 && j == 2)
                    usage.DeletedAt = now.AddMinutes(-30);
                db.ServiceUsages.Add(usage);
                usages.Add(usage);
            }
            usagesByStay[stay.Id] = usages;
        }
        await db.SaveChangesAsync();

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F07-04/F09-04/F09-05 | 16 Invoices (12 hoan tat,
        // 2 Draft dang luu tru, 2 Finalized dang luu tru).
        // InvoiceLines va TotalAmount tinh tu gia da chot + snapshot dich vu.
        // --------------------------------------------------------------------
        var invoices = new List<Invoice>();
        for (var i = 0; i < stays.Count; i++)
        {
            var activeIndex = i - completed.Count;
            var invoiceNeeded = i < completed.Count || activeIndex is 1 or 2 or 3 or 4;
            if (!invoiceNeeded) continue;

            var stay = stays[i];
            var reservation = allStayReservations[i];
            var isFinalized = i < completed.Count || activeIndex is 3 or 4;
            var room = rooms.Single(r => r.Id == reservation.RoomId);
            var mainGuest = guests.Single(g => g.Id == reservation.GuestId);
            var nights = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
            var invoice = new Invoice
            {
                InvoiceCode = $"INV-{i + 1:000}",
                StayId = stay.Id,
                Status = isFinalized ? InvoiceStatus.Finalized : InvoiceStatus.Draft,
                BillingFromDate = reservation.CheckInDate,
                BillingToDate = reservation.CheckOutDate,
                GuestNameSnapshot = mainGuest.FullName,
                GuestDocumentSnapshot = mainGuest.DocumentNumber,
                RoomCodeSnapshot = room.RoomCode,
                CreatedByAccountId = receptionists[i % 2].Id,
                FinalizedAt = isFinalized
                    ? (stay.CheckedOutAt?.AddHours(-2) ?? now.AddHours(-2)) : null,
                FinalizedByAccountId = isFinalized ? receptionists[i % 2].Id : null,
                Notes = isFinalized ? "Hoa don da chot - demo" : "Hoa don nhap - demo"
            };
            invoice.Lines.Add(new InvoiceLine
            {
                LineType = InvoiceLineType.RoomCharge,
                Description = $"Tien phong {room.RoomCode}",
                Unit = "dem",
                Quantity = nights,
                UnitPrice = reservation.NightlyRate
            });
            var total = nights * reservation.NightlyRate;
            foreach (var usage in usagesByStay[stay.Id].Where(u => u.DeletedAt == null))
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    LineType = InvoiceLineType.ServiceCharge,
                    ServiceUsageId = usage.Id,
                    Description = usage.ServiceNameSnapshot,
                    Unit = usage.UnitSnapshot,
                    Quantity = usage.Quantity,
                    UnitPrice = usage.UnitPrice
                });
                total += decimal.Round(usage.Quantity * usage.UnitPrice, 2,
                    MidpointRounding.AwayFromZero);
            }
            invoice.TotalAmount = total;
            db.Invoices.Add(invoice);
            invoices.Add(invoice);
        }
        await db.SaveChangesAsync();

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F07-05/F07-06/F11-03 | Payment tuong ung so tien thuc thu.
        // Completed: thanh toan du. Active #4: tra mot phan; Active #5: tra du.
        // --------------------------------------------------------------------
        var payments = new List<Payment>();
        foreach (var invoice in invoices.Where(v => v.Status == InvoiceStatus.Finalized))
        {
            var stayIndex = stays.FindIndex(s => s.Id == invoice.StayId);
            var isCompleted = stayIndex < completed.Count;
            var amountFirst = isCompleted
                ? (stayIndex % 3 == 0 ? invoice.TotalAmount / 2m : invoice.TotalAmount)
                : (stayIndex - completed.Count == 3 ? invoice.TotalAmount / 2m : invoice.TotalAmount);
            amountFirst = decimal.Round(amountFirst, 2, MidpointRounding.AwayFromZero);
            var paidAt = isCompleted
                ? stays[stayIndex].CheckedOutAt!.Value.AddMinutes(-30)
                : now.AddMinutes(-10);

            void AddPayment(decimal amount, int part)
            {
                var payment = new Payment
                {
                    ReceiptCode = $"RC-{stayIndex + 1:000}-{part}",
                    InvoiceId = invoice.Id,
                    IdempotencyKey = Guid.NewGuid(),
                    Amount = amount,
                    Method = (PaymentMethod)(1 + (stayIndex + part) % 3),
                    PaidAt = paidAt.AddMinutes(part),
                    RecordedByAccountId = receptionists[stayIndex % 2].Id,
                    CreatedAt = now,
                    ReferenceNumber = $"DEMO-{stayIndex + 1:000}-{part}",
                    Notes = "Thanh toan demo tai quay"
                };
                db.Payments.Add(payment);
                payments.Add(payment);
            }
            AddPayment(amountFirst, 1);
            if (isCompleted && amountFirst < invoice.TotalAmount)
                AddPayment(invoice.TotalAmount - amountFirst, 2);
        }
        await db.SaveChangesAsync();

        // --------------------------------------------------------------------
        // NhaNTCE181298 | F11-04/F07-01 | 21 HotelTasks, nhieu loai va trang thai.
        // Housekeeping bat buoc HousekeepingKind, Maintenance de null.
        // --------------------------------------------------------------------
        var tasks = new List<HotelTask>();
        for (var i = 0; i < 12; i++)
        {
            var cleaningDate = completed[i].CheckOutDate;
            tasks.Add(new HotelTask
            {
                TaskCode = $"TASK-H{i + 1:000}",
                RoomId = rooms[i].Id,
                TaskType = HotelTaskType.Housekeeping,
                HousekeepingKind = i % 2 == 0 ? HousekeepingKind.Cleaning : HousekeepingKind.Inspection,
                Title = $"Ve sinh / kiem tra phong {rooms[i].RoomCode}",
                Description = "Cong viec da hoan tat trong lich su",
                Priority = TaskPriority.Normal,
                Status = HotelTaskStatus.Completed,
                AssignedToAccountId = roomStaff[i % 2].Id,
                CreatedByAccountId = receptionists[i % 2].Id,
                StartedAt = AtVietnamTime(cleaningDate, 12),
                CompletedAt = AtVietnamTime(cleaningDate, 14),
                CompletedByAccountId = roomStaff[i % 2].Id,
                CompletionNotes = "Da xu ly"
            });
        }

        var extraTaskSpecs = new (string RoomCode, HotelTaskStatus Status,
            HousekeepingKind Kind)[]
        {
            ("P104", HotelTaskStatus.Pending, HousekeepingKind.Cleaning),
            ("P204", HotelTaskStatus.InProgress, HousekeepingKind.Cleaning),
            ("P103", HotelTaskStatus.Assigned, HousekeepingKind.Inspection),
            ("P402", HotelTaskStatus.Cancelled, HousekeepingKind.Inspection),
            ("P401", HotelTaskStatus.Completed, HousekeepingKind.Inspection),
            ("P202", HotelTaskStatus.Completed, HousekeepingKind.Cleaning)
        };
        for (var i = 0; i < extraTaskSpecs.Length; i++)
        {
            var spec = extraTaskSpecs[i];
            var hasStarted = spec.Status is HotelTaskStatus.InProgress or HotelTaskStatus.Completed;
            var isDone = spec.Status == HotelTaskStatus.Completed;
            tasks.Add(new HotelTask
            {
                TaskCode = $"TASK-X{i + 1:000}",
                RoomId = roomByCode[spec.RoomCode].Id,
                TaskType = HotelTaskType.Housekeeping,
                HousekeepingKind = spec.Kind,
                Title = $"Cong viec phong {spec.RoomCode}",
                Description = "Du lieu test bo loc trang thai task",
                Priority = i % 2 == 0 ? TaskPriority.High : TaskPriority.Normal,
                Status = spec.Status,
                AssignedToAccountId = spec.Status == HotelTaskStatus.Pending ? null : roomStaff[i % 2].Id,
                CreatedByAccountId = receptionists[i % 2].Id,
                StartedAt = hasStarted ? now.AddHours(-5) : null,
                CompletedAt = isDone ? now.AddHours(-1) : null,
                CompletedByAccountId = isDone ? roomStaff[i % 2].Id : null
            });
        }

        HotelTask NewMaintenance(string code, string roomCode, HotelTaskStatus status)
        {
            return new HotelTask
            {
                TaskCode = code,
                RoomId = roomByCode[roomCode].Id,
                TaskType = HotelTaskType.Maintenance,
                HousekeepingKind = null,
                Title = $"Bao tri phong {roomCode}",
                Description = "Kiem tra va sua chua co so vat chat",
                Priority = TaskPriority.High,
                Status = status,
                AssignedToAccountId = roomStaff[0].Id,
                CreatedByAccountId = receptionists[0].Id,
                StartedAt = status == HotelTaskStatus.InProgress ? now.AddDays(-1) : null,
                CompletedAt = status == HotelTaskStatus.Completed ? now.AddDays(-10) : null,
                CompletedByAccountId = status == HotelTaskStatus.Completed ? roomStaff[0].Id : null
            };
        }
        var currentMaintenance = NewMaintenance("TASK-M001", "P304", HotelTaskStatus.InProgress);
        var futureMaintenance = NewMaintenance("TASK-M002", "P303", HotelTaskStatus.Assigned);
        var oldMaintenance = NewMaintenance("TASK-M003", "P203", HotelTaskStatus.Completed);
        tasks.AddRange(new[] { currentMaintenance, futureMaintenance, oldMaintenance });
        db.HotelTasks.AddRange(tasks);
        await db.SaveChangesAsync();

        // NhaNTCE181298 | F11-04 | Giu ngay tao lich su de loc task theo CreatedAt.
        foreach (var task in tasks.Where(t => t.Status == HotelTaskStatus.Completed))
        {
            task.CreatedAt = (task.StartedAt ?? task.CompletedAt!.Value).AddHours(-2);
            db.Entry(task).Property(t => t.CreatedAt).IsModified = true;
        }
        await db.SaveChangesAsync();

        // NhaNTCE181298 | F07-01/F11-01 | 4 RoomBlocks: dang khoa, sap khoa,
        // da giai phong va da huy. FK task/room phai cung phong.
        db.RoomBlocks.AddRange(
            new RoomBlock
            {
                RoomId = roomByCode["P304"].Id,
                HotelTaskId = currentMaintenance.Id,
                StartAt = AtVietnamTime(today.AddDays(-1), 8),
                EndAt = AtVietnamTime(today.AddDays(5), 18),
                Reason = "Dang bao tri dieu hoa",
                CreatedByAccountId = receptionists[0].Id
            },
            new RoomBlock
            {
                RoomId = roomByCode["P303"].Id,
                HotelTaskId = futureMaintenance.Id,
                StartAt = AtVietnamTime(today.AddDays(20), 8),
                EndAt = AtVietnamTime(today.AddDays(22), 18),
                Reason = "Bao tri theo lich",
                CreatedByAccountId = receptionists[0].Id
            },
            new RoomBlock
            {
                RoomId = roomByCode["P203"].Id,
                HotelTaskId = oldMaintenance.Id,
                StartAt = AtVietnamTime(today.AddDays(-15), 8),
                EndAt = AtVietnamTime(today.AddDays(-10), 18),
                ReleasedAt = AtVietnamTime(today.AddDays(-11), 15),
                Reason = "Bao tri da hoan tat",
                CreatedByAccountId = receptionists[0].Id
            },
            new RoomBlock
            {
                RoomId = roomByCode["P402"].Id,
                StartAt = AtVietnamTime(today.AddDays(9), 8),
                EndAt = AtVietnamTime(today.AddDays(11), 18),
                CancelledAt = now.AddMinutes(-5),
                Reason = "Lich bao tri da huy",
                CreatedByAccountId = receptionists[1].Id
            }
        );

        // NhaNTCE181298 | F07/F09/F11 | Nhat ky mau co tac nhan + ma doi tuong.
        var auditLogs = new List<AuditLog>();
        void AddAudit(int actorId, string actorName, string action,
            string entityType, string entityId, string code, string summary, DateTime at)
        {
            auditLogs.Add(new AuditLog
            {
                ActorAccountId = actorId,
                ActorUsernameSnapshot = actorName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityCodeSnapshot = code,
                OccurredAt = at,
                Summary = summary,
                CorrelationId = $"demo-seed-{entityType.ToLowerInvariant()}"
            });
        }
        for (var i = 0; i < stays.Count; i++)
        {
            var actor = receptionists[i % 2];
            AddAudit(actor.Id, actor.Username, "CheckIn", "Stay",
                stays[i].Id.ToString(), stays[i].StayCode,
                "Nhan phong mau", stays[i].CheckedInAt);
        }
        foreach (var invoice in invoices)
        {
            var actor = receptionists.Single(a => a.Id == invoice.CreatedByAccountId);
            AddAudit(actor.Id, actor.Username, "CreateInvoice", "Invoice",
                invoice.Id.ToString(), invoice.InvoiceCode,
                "Tao hoa don noi bo mau", invoice.FinalizedAt ?? now.AddMinutes(-20));
        }
        foreach (var payment in payments)
        {
            var actor = receptionists.Single(a => a.Id == payment.RecordedByAccountId);
            AddAudit(actor.Id, actor.Username, "RecordPayment", "Payment",
                payment.Id.ToString(), payment.ReceiptCode,
                "Ghi nhan thanh toan mau", payment.PaidAt);
        }
        db.AuditLogs.AddRange(auditLogs);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine($"[DEMO SEED] Hoan tat ngay {today:dd/MM/yyyy}: " +
            $"{5} Accounts, {types.Length} RoomTypes, {rooms.Length} Rooms, " +
            $"{guests.Length} Guests, {reservationCounter} Reservations, " +
            $"{stays.Count} Stays, {hotelServices.Length} HotelServices, " +
            $"{usagesByStay.Values.Sum(v => v.Count)} ServiceUsages, " +
            $"{invoices.Count} Invoices, {payments.Count} Payments, " +
            $"{tasks.Count} HotelTasks, 4 RoomBlocks, {auditLogs.Count} AuditLogs.");
    }

    // NhaNTCE181298 | F07/F11 | Chuyen ngay nghiep vu Viet Nam UTC+7 sang UTC de luu SQL.
    private static DateTime AtVietnamTime(DateOnly date, int hour) =>
        DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, 0)).AddHours(-7), DateTimeKind.Utc);
}
