using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> b)
    {
        b.ToTable("RoomTypes", t =>
        {
            t.HasCheckConstraint("CK_RoomTypes_Capacity", "[Capacity] > 0");
            t.HasCheckConstraint("CK_RoomTypes_BasePrice", "[BasePricePerNight] >= 0");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasColumnType("varchar(20)").IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.BasePricePerNight).HasColumnType("decimal(18,2)");
        b.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.BasePricePerNight);
    }
}