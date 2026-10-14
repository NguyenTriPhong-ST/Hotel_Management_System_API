using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class ServiceUsageConfiguration : IEntityTypeConfiguration<ServiceUsage>
{
    public void Configure(EntityTypeBuilder<ServiceUsage> b)
    {
        b.ToTable("ServiceUsages", t =>
        {
            t.HasCheckConstraint("CK_ServiceUsages_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_ServiceUsages_UnitPrice", "[UnitPrice] >= 0");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.ServiceNameSnapshot).HasMaxLength(150).IsRequired();
        b.Property(x => x.UnitSnapshot).HasMaxLength(30).IsRequired();
        b.Property(x => x.Quantity).HasColumnType("decimal(10,2)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.UsedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Stay).WithMany(s => s.ServiceUsages).HasForeignKey(x => x.StayId);
        b.HasOne(x => x.HotelService).WithMany(s => s.ServiceUsages).HasForeignKey(x => x.HotelServiceId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.RecordedByAccountId);

        b.HasIndex(x => new { x.StayId, x.UsedAt });
        b.HasIndex(x => new { x.HotelServiceId, x.UsedAt });
    }
}