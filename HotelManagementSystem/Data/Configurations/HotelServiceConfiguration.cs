using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class HotelServiceConfiguration : IEntityTypeConfiguration<HotelService>
{
    public void Configure(EntityTypeBuilder<HotelService> b)
    {
        b.ToTable("HotelServices", t =>
            t.HasCheckConstraint("CK_HotelServices_CurrentPrice", "[CurrentPrice] >= 0"));
        b.HasKey(x => x.Id);

        b.Property(x => x.ServiceCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.ServiceCode).IsUnique();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        b.Property(x => x.CurrentPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.IsActive, x.CurrentPrice });
    }
}