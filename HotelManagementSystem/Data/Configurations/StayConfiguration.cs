using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class StayConfiguration : IEntityTypeConfiguration<Stay>
{
    public void Configure(EntityTypeBuilder<Stay> b)
    {
        b.ToTable("Stays", t =>
            t.HasCheckConstraint("CK_Stays_CheckOutAfterCheckIn",
                "[CheckedOutAt] IS NULL OR [CheckedOutAt] >= [CheckedInAt]"));
        b.HasKey(x => x.Id);

        b.Property(x => x.StayCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.StayCode).IsUnique();

        b.Property(x => x.CheckedInAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CheckedOutAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        // Khóa ngoại ghép: stay phải ở đúng phòng của reservation
        // Stays(ReservationId, RoomId) -> Reservations(Id, RoomId)
        b.HasOne(x => x.Reservation)
            .WithOne(r => r.Stay)
            .HasForeignKey<Stay>(x => new { x.ReservationId, x.RoomId })
            .HasPrincipalKey<Reservation>(r => new { r.Id, r.RoomId });

        // Chống check-in lặp: mỗi reservation chỉ có tối đa một stay
        b.HasIndex(x => x.ReservationId).IsUnique();

        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CheckedInByAccountId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CheckedOutByAccountId);

        // Mỗi phòng chỉ có một stay đang hoạt động (chưa check-out)
        b.HasIndex(x => x.RoomId)
            .IsUnique()
            .HasFilter("[CheckedOutAt] IS NULL")
            .HasDatabaseName("UX_Stays_RoomId_Active");

        b.HasIndex(x => x.CheckedInAt);
    }
}