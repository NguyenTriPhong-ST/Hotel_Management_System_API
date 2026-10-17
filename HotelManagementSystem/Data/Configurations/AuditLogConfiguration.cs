using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);

        b.Property(x => x.ActorUsernameSnapshot).HasMaxLength(50);
        b.Property(x => x.Action).HasColumnType("varchar(50)").IsRequired();
        b.Property(x => x.EntityType).HasColumnType("varchar(100)").IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        b.Property(x => x.EntityCodeSnapshot).HasMaxLength(50);
        b.Property(x => x.OccurredAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
        b.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CorrelationId).HasColumnType("varchar(100)");

        // Chỉ ActorAccountId là khóa ngoại thật; EntityId không phải FK vì trỏ tới nhiều loại bảng
        b.HasOne(x => x.ActorAccount).WithMany().HasForeignKey(x => x.ActorAccountId);

        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => new { x.ActorAccountId, x.OccurredAt });
        b.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
    }
}