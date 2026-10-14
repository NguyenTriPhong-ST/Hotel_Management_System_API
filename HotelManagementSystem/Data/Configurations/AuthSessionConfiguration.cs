using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> b)
    {
        b.ToTable("AuthSessions");
        b.HasKey(x => x.Id);

        b.Property(x => x.IssuedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.ExpiresAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RevokedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId);

        b.HasIndex(x => new { x.AccountId, x.RevokedAt, x.ExpiresAt });
    }
}