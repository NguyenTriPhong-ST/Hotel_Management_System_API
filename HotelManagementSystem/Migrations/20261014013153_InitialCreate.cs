using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(50)", nullable: false),
                    NormalizedUsername = table.Column<string>(type: "varchar(50)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_Role", "[Role] IN (1,2,3)");
                });

            migrationBuilder.CreateTable(
                name: "Guests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DocumentCountryCode = table.Column<string>(type: "varchar(3)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                    table.CheckConstraint("CK_Guests_DocumentGroup", "([DocumentType] IS NULL AND [DocumentCountryCode] IS NULL AND [DocumentNumber] IS NULL) OR ([DocumentType] IS NOT NULL AND [DocumentCountryCode] IS NOT NULL AND [DocumentNumber] IS NOT NULL)");
                    table.CheckConstraint("CK_Guests_DocumentType", "[DocumentType] IS NULL OR [DocumentType] IN (1,2)");
                });

            migrationBuilder.CreateTable(
                name: "HotelServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelServices", x => x.Id);
                    table.CheckConstraint("CK_HotelServices_CurrentPrice", "[CurrentPrice] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(20)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    BasePricePerNight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.Id);
                    table.CheckConstraint("CK_RoomTypes_BasePrice", "[BasePricePerNight] >= 0");
                    table.CheckConstraint("CK_RoomTypes_Capacity", "[Capacity] > 0");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorAccountId = table.Column<int>(type: "int", nullable: true),
                    ActorUsernameSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomCode = table.Column<string>(type: "varchar(20)", nullable: false),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    FloorNumber = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OperationalStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    HousekeepingStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.CheckConstraint("CK_Rooms_HousekeepingStatus", "[HousekeepingStatus] IN (1,2,3,4)");
                    table.CheckConstraint("CK_Rooms_OperationalStatus", "[OperationalStatus] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HotelTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<byte>(type: "tinyint", nullable: false),
                    HousekeepingKind = table.Column<byte>(type: "tinyint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AssignedToAccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CompletedByAccountId = table.Column<int>(type: "int", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelTasks", x => x.Id);
                    table.UniqueConstraint("AK_HotelTasks_Id_RoomId", x => new { x.Id, x.RoomId });
                    table.CheckConstraint("CK_HotelTasks_HousekeepingKind", "([TaskType] = 1 AND [HousekeepingKind] IN (1,2)) OR ([TaskType] = 2 AND [HousekeepingKind] IS NULL)");
                    table.CheckConstraint("CK_HotelTasks_Priority", "[Priority] IN (1,2,3,4)");
                    table.CheckConstraint("CK_HotelTasks_Status", "[Status] IN (1,2,3,4,5)");
                    table.CheckConstraint("CK_HotelTasks_TaskType", "[TaskType] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_HotelTasks_Accounts_AssignedToAccountId",
                        column: x => x.AssignedToAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelTasks_Accounts_CompletedByAccountId",
                        column: x => x.CompletedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelTasks_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelTasks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    GuestId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    CapacitySnapshot = table.Column<int>(type: "int", nullable: false),
                    NightlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CancelledByAccountId = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.UniqueConstraint("AK_Reservations_Id_RoomId", x => new { x.Id, x.RoomId });
                    table.CheckConstraint("CK_Reservations_CapacitySnapshot", "[CapacitySnapshot] > 0");
                    table.CheckConstraint("CK_Reservations_Dates", "[CheckOutDate] > [CheckInDate]");
                    table.CheckConstraint("CK_Reservations_GuestsWithinCapacity", "[NumberOfGuests] <= [CapacitySnapshot]");
                    table.CheckConstraint("CK_Reservations_NightlyRate", "[NightlyRate] >= 0");
                    table.CheckConstraint("CK_Reservations_NumberOfGuests", "[NumberOfGuests] > 0");
                    table.CheckConstraint("CK_Reservations_Status", "[Status] IN (1,2,3,4,5)");
                    table.ForeignKey(
                        name: "FK_Reservations_Accounts_CancelledByAccountId",
                        column: x => x.CancelledByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    HotelTaskId = table.Column<int>(type: "int", nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomBlocks", x => x.Id);
                    table.CheckConstraint("CK_RoomBlocks_EndAfterStart", "[EndAt] IS NULL OR [EndAt] > [StartAt]");
                    table.ForeignKey(
                        name: "FK_RoomBlocks_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomBlocks_HotelTasks_HotelTaskId_RoomId",
                        columns: x => new { x.HotelTaskId, x.RoomId },
                        principalTable: "HotelTasks",
                        principalColumns: new[] { "Id", "RoomId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomBlocks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StayCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CheckedInByAccountId = table.Column<int>(type: "int", nullable: false),
                    CheckedOutByAccountId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stays", x => x.Id);
                    table.CheckConstraint("CK_Stays_CheckOutAfterCheckIn", "[CheckedOutAt] IS NULL OR [CheckedOutAt] >= [CheckedInAt]");
                    table.ForeignKey(
                        name: "FK_Stays_Accounts_CheckedInByAccountId",
                        column: x => x.CheckedInByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stays_Accounts_CheckedOutByAccountId",
                        column: x => x.CheckedOutByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stays_Reservations_ReservationId_RoomId",
                        columns: x => new { x.ReservationId, x.RoomId },
                        principalTable: "Reservations",
                        principalColumns: new[] { "Id", "RoomId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stays_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    StayId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    BillingFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BillingToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GuestNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GuestDocumentSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RoomCodeSnapshot = table.Column<string>(type: "varchar(20)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    FinalizedByAccountId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoices_BillingDates", "[BillingToDate] > [BillingFromDate]");
                    table.CheckConstraint("CK_Invoices_Status", "[Status] IN (1,2)");
                    table.CheckConstraint("CK_Invoices_TotalAmount", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Invoices_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Accounts_FinalizedByAccountId",
                        column: x => x.FinalizedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Stays_StayId",
                        column: x => x.StayId,
                        principalTable: "Stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StayId = table.Column<int>(type: "int", nullable: false),
                    HotelServiceId = table.Column<int>(type: "int", nullable: false),
                    ServiceNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnitSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RecordedByAccountId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceUsages", x => x.Id);
                    table.CheckConstraint("CK_ServiceUsages_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_ServiceUsages_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_ServiceUsages_Accounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceUsages_HotelServices_HotelServiceId",
                        column: x => x.HotelServiceId,
                        principalTable: "HotelServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceUsages_Stays_StayId",
                        column: x => x.StayId,
                        principalTable: "Stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StayGuests",
                columns: table => new
                {
                    StayId = table.Column<int>(type: "int", nullable: false),
                    GuestId = table.Column<int>(type: "int", nullable: false),
                    IsMainGuest = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayGuests", x => new { x.StayId, x.GuestId });
                    table.ForeignKey(
                        name: "FK_StayGuests_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StayGuests_Stays_StayId",
                        column: x => x.StayId,
                        principalTable: "Stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordedByAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Payments_Method", "[Method] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_Payments_Accounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<byte>(type: "tinyint", nullable: false),
                    ServiceUsageId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "ROUND([Quantity] * [UnitPrice], 2)", stored: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_InvoiceLines_LineType", "[LineType] IN (1,2)");
                    table.CheckConstraint("CK_InvoiceLines_LineTypeUsage", "([LineType] = 1 AND [ServiceUsageId] IS NULL) OR ([LineType] = 2 AND [ServiceUsageId] IS NOT NULL)");
                    table.CheckConstraint("CK_InvoiceLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_InvoiceLines_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_ServiceUsages_ServiceUsageId",
                        column: x => x.ServiceUsageId,
                        principalTable: "ServiceUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_NormalizedUsername",
                table: "Accounts",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Role_IsActive",
                table: "Accounts",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorAccountId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "ActorAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_AccountId_RevokedAt_ExpiresAt",
                table: "AuthSessions",
                columns: new[] { "AccountId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_DocumentType_DocumentCountryCode_DocumentNumber",
                table: "Guests",
                columns: new[] { "DocumentType", "DocumentCountryCode", "DocumentNumber" },
                unique: true,
                filter: "[DocumentNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_PhoneNumber",
                table: "Guests",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HotelServices_IsActive_CurrentPrice",
                table: "HotelServices",
                columns: new[] { "IsActive", "CurrentPrice" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelServices_ServiceCode",
                table: "HotelServices",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelTasks_AssignedToAccountId_Status_CreatedAt",
                table: "HotelTasks",
                columns: new[] { "AssignedToAccountId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelTasks_CompletedByAccountId",
                table: "HotelTasks",
                column: "CompletedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelTasks_CreatedByAccountId",
                table: "HotelTasks",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelTasks_RoomId_TaskType_Status",
                table: "HotelTasks",
                columns: new[] { "RoomId", "TaskType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelTasks_TaskCode",
                table: "HotelTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_InvoiceLines_RoomCharge",
                table: "InvoiceLines",
                column: "InvoiceId",
                unique: true,
                filter: "[LineType] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_InvoiceLines_ServiceUsage",
                table: "InvoiceLines",
                column: "ServiceUsageId",
                unique: true,
                filter: "[ServiceUsageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByAccountId",
                table: "Invoices",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FinalizedByAccountId",
                table: "Invoices",
                column: "FinalizedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceCode",
                table: "Invoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StayId",
                table: "Invoices",
                column: "StayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidAt_Method",
                table: "Payments",
                columns: new[] { "PaidAt", "Method" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptCode",
                table: "Payments",
                column: "ReceiptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RecordedByAccountId",
                table: "Payments",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CancelledByAccountId",
                table: "Reservations",
                column: "CancelledByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedByAccountId",
                table: "Reservations",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_GuestId_CheckInDate",
                table: "Reservations",
                columns: new[] { "GuestId", "CheckInDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationCode",
                table: "Reservations",
                column: "ReservationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_Status_CheckInDate_CheckOutDate",
                table: "Reservations",
                columns: new[] { "RoomId", "Status", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomTypeId",
                table: "Reservations",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomBlocks_CreatedByAccountId",
                table: "RoomBlocks",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomBlocks_HotelTaskId_RoomId",
                table: "RoomBlocks",
                columns: new[] { "HotelTaskId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomBlocks_RoomId_StartAt",
                table: "RoomBlocks",
                columns: new[] { "RoomId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomCode",
                table: "Rooms",
                column: "RoomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeId_OperationalStatus_HousekeepingStatus",
                table: "Rooms",
                columns: new[] { "RoomTypeId", "OperationalStatus", "HousekeepingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_BasePricePerNight",
                table: "RoomTypes",
                column: "BasePricePerNight");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_Code",
                table: "RoomTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUsages_HotelServiceId_UsedAt",
                table: "ServiceUsages",
                columns: new[] { "HotelServiceId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUsages_RecordedByAccountId",
                table: "ServiceUsages",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUsages_StayId_UsedAt",
                table: "ServiceUsages",
                columns: new[] { "StayId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StayGuests_GuestId_StayId",
                table: "StayGuests",
                columns: new[] { "GuestId", "StayId" });

            migrationBuilder.CreateIndex(
                name: "UX_StayGuests_MainGuest",
                table: "StayGuests",
                column: "StayId",
                unique: true,
                filter: "[IsMainGuest] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Stays_CheckedInAt",
                table: "Stays",
                column: "CheckedInAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stays_CheckedInByAccountId",
                table: "Stays",
                column: "CheckedInByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Stays_CheckedOutByAccountId",
                table: "Stays",
                column: "CheckedOutByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Stays_ReservationId",
                table: "Stays",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stays_ReservationId_RoomId",
                table: "Stays",
                columns: new[] { "ReservationId", "RoomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stays_StayCode",
                table: "Stays",
                column: "StayCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Stays_RoomId_Active",
                table: "Stays",
                column: "RoomId",
                unique: true,
                filter: "[CheckedOutAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RoomBlocks");

            migrationBuilder.DropTable(
                name: "StayGuests");

            migrationBuilder.DropTable(
                name: "ServiceUsages");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "HotelTasks");

            migrationBuilder.DropTable(
                name: "HotelServices");

            migrationBuilder.DropTable(
                name: "Stays");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "RoomTypes");
        }
    }
}
