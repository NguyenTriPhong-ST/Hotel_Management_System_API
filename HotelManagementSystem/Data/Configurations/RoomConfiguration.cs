using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms", t =>
        {
            t.HasCheckConstraint("CK_Rooms_OperationalStatus", "[OperationalStatus] IN (1,2)");
            t.HasCheckConstraint("CK_Rooms_HousekeepingStatus", "[HousekeepingStatus] IN (1,2,3,4)");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.RoomCode).HasColumnType("varchar(20)").IsRequired();
        b.HasIndex(x => x.RoomCode).IsUnique();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.OperationalStatus).HasConversion<byte>();
        b.Property(x => x.HousekeepingStatus).HasConversion<byte>();
        b.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.RoomType).WithMany(t => t.Rooms).HasForeignKey(x => x.RoomTypeId);

        b.HasIndex(x => new { x.RoomTypeId, x.OperationalStatus, x.HousekeepingStatus });
    }
}