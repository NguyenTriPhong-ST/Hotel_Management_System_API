using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments", t =>
        {
            t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Payments_Method", "[Method] IN (1,2,3)");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.ReceiptCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.ReceiptCode).IsUnique();

        // Chống ghi nhận trùng khi client gửi lại request
        b.HasIndex(x => x.IdempotencyKey).IsUnique();

        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Method).HasConversion<byte>();
        b.Property(x => x.PaidAt).HasColumnType("datetime2(3)");
        b.Property(x => x.ReferenceNumber).HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.Invoice).WithMany(i => i.Payments).HasForeignKey(x => x.InvoiceId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.RecordedByAccountId);

        b.HasIndex(x => x.InvoiceId);
        b.HasIndex(x => new { x.PaidAt, x.Method });
    }
}