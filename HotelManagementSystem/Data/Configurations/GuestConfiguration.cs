using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> b)
    {
        b.ToTable("Guests", t =>
        {
            t.HasCheckConstraint("CK_Guests_DocumentType", "[DocumentType] IS NULL OR [DocumentType] IN (1,2)");

            // 3 trường giấy tờ: cùng có giá trị hoặc cùng để trống
            t.HasCheckConstraint("CK_Guests_DocumentGroup",
                "([DocumentType] IS NULL AND [DocumentCountryCode] IS NULL AND [DocumentNumber] IS NULL) " +
                "OR ([DocumentType] IS NOT NULL AND [DocumentCountryCode] IS NOT NULL AND [DocumentNumber] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.DateOfBirth).HasColumnType("date");
        b.Property(x => x.PhoneNumber).HasColumnType("varchar(20)");
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.DocumentType).HasConversion<byte?>();
        b.Property(x => x.DocumentNumber).HasMaxLength(30);
        b.Property(x => x.DocumentCountryCode).HasColumnType("varchar(3)");
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        // Unique có điều kiện: chỉ áp dụng khi đã khai báo giấy tờ
        b.HasIndex(x => new { x.DocumentType, x.DocumentCountryCode, x.DocumentNumber })
            .IsUnique()
            .HasFilter("[DocumentNumber] IS NOT NULL");

        b.HasIndex(x => x.PhoneNumber);
    }
}