using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines", t =>
        {
            t.HasCheckConstraint("CK_InvoiceLines_LineType", "[LineType] IN (1,2)");
            t.HasCheckConstraint("CK_InvoiceLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_InvoiceLines_UnitPrice", "[UnitPrice] >= 0");

            // Dòng tiền phòng không có ServiceUsageId; dòng dịch vụ bắt buộc có
            t.HasCheckConstraint("CK_InvoiceLines_LineTypeUsage",
                "([LineType] = 1 AND [ServiceUsageId] IS NULL) OR ([LineType] = 2 AND [ServiceUsageId] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.LineType).HasConversion<byte>();
        b.Property(x => x.Description).HasMaxLength(250).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        b.Property(x => x.Quantity).HasColumnType("decimal(10,2)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

        // Cột tính toán lưu sẵn trong DB: ROUND(Quantity * UnitPrice, 2)
        b.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .HasComputedColumnSql("ROUND([Quantity] * [UnitPrice], 2)", stored: true);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Invoice).WithMany(i => i.Lines).HasForeignKey(x => x.InvoiceId);
        b.HasOne(x => x.ServiceUsage).WithOne(u => u.InvoiceLine).HasForeignKey<InvoiceLine>(x => x.ServiceUsageId);

        // Một lượt dịch vụ chỉ được tính phí một lần
        b.HasIndex(x => x.ServiceUsageId)
            .IsUnique()
            .HasFilter("[ServiceUsageId] IS NOT NULL")
            .HasDatabaseName("UX_InvoiceLines_ServiceUsage");

        // Một hóa đơn có tối đa một dòng tiền phòng
        b.HasIndex(x => x.InvoiceId)
            .IsUnique()
            .HasFilter("[LineType] = 1")
            .HasDatabaseName("UX_InvoiceLines_RoomCharge");
    }
}