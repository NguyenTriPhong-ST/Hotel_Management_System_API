using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("Reservations", t =>
        {
            t.HasCheckConstraint("CK_Reservations_Dates", "[CheckOutDate] > [CheckInDate]");
            t.HasCheckConstraint("CK_Reservations_NumberOfGuests", "[NumberOfGuests] > 0");
            t.HasCheckConstraint("CK_Reservations_CapacitySnapshot", "[CapacitySnapshot] > 0");
            t.HasCheckConstraint("CK_Reservations_GuestsWithinCapacity", "[NumberOfGuests] <= [CapacitySnapshot]");
            t.HasCheckConstraint("CK_Reservations_NightlyRate", "[NightlyRate] >= 0");
            t.HasCheckConstraint("CK_Reservations_Status", "[Status] IN (1,2,3,4,5)");
        });
        b.HasKey(x => x.Id);

        // Dùng cho khóa ngoại ghép Stays(ReservationId, RoomId) -> Reservations(Id, RoomId) ở bước 6.3
        b.HasAlternateKey(x => new { x.Id, x.RoomId });

        b.Property(x => x.ReservationCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.ReservationCode).IsUnique();

        b.Property(x => x.CheckInDate).HasColumnType("date");
        b.Property(x => x.CheckOutDate).HasColumnType("date");
        b.Property(x => x.NightlyRate).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.CancelledAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CancellationReason).HasMaxLength(500);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        // Quan hệ
        b.HasOne(x => x.Guest).WithMany(g => g.Reservations).HasForeignKey(x => x.GuestId);
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
        b.HasOne(x => x.RoomType).WithMany().HasForeignKey(x => x.RoomTypeId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CancelledByAccountId);

        // Chỉ mục kiểm tra lịch phòng và xem lịch đặt của khách
        b.HasIndex(x => new { x.RoomId, x.Status, x.CheckInDate, x.CheckOutDate });
        b.HasIndex(x => new { x.GuestId, x.CheckInDate });
    }
}