using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices", t =>
        {
            t.HasCheckConstraint("CK_Invoices_Status", "[Status] IN (1,2)");
            t.HasCheckConstraint("CK_Invoices_BillingDates", "[BillingToDate] > [BillingFromDate]");
            t.HasCheckConstraint("CK_Invoices_TotalAmount", "[TotalAmount] >= 0");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.InvoiceCode).IsUnique();

        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.BillingFromDate).HasColumnType("date");
        b.Property(x => x.BillingToDate).HasColumnType("date");
        b.Property(x => x.GuestNameSnapshot).HasMaxLength(150).IsRequired();
        b.Property(x => x.GuestDocumentSnapshot).HasMaxLength(30);
        b.Property(x => x.RoomCodeSnapshot).HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.FinalizedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        // Một stay có tối đa một hóa đơn
        b.HasOne(x => x.Stay).WithOne(s => s.Invoice).HasForeignKey<Invoice>(x => x.StayId);
        b.HasIndex(x => x.StayId).IsUnique();

        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.FinalizedByAccountId);
    }
}