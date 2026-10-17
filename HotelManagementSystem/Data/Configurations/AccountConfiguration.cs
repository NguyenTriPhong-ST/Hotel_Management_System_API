using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("Accounts", t => t.HasCheckConstraint("CK_Accounts_Role", "[Role] IN (1,2,3)"));
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasColumnType("varchar(50)").IsRequired();
        b.Property(x => x.NormalizedUsername).HasColumnType("varchar(50)").IsRequired();
        b.HasIndex(x => x.NormalizedUsername).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.PhoneNumber).HasColumnType("varchar(20)");
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Role).HasConversion<byte>();
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.Role, x.IsActive });
    }
}