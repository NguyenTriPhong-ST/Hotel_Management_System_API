using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class RoomBlockConfiguration : IEntityTypeConfiguration<RoomBlock>
{
    public void Configure(EntityTypeBuilder<RoomBlock> b)
    {
        b.ToTable("RoomBlocks", t =>
            t.HasCheckConstraint("CK_RoomBlocks_EndAfterStart", "[EndAt] IS NULL OR [EndAt] > [StartAt]"));
        b.HasKey(x => x.Id);

        b.Property(x => x.StartAt).HasColumnType("datetime2(3)");
        b.Property(x => x.EndAt).HasColumnType("datetime2(3)");
        b.Property(x => x.ReleasedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CancelledAt).HasColumnType("datetime2(3)");
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);

        // Khóa ngoại ghép: task liên quan phải thuộc cùng phòng
        // RoomBlocks(HotelTaskId, RoomId) -> HotelTasks(Id, RoomId)
        b.HasOne(x => x.HotelTask)
            .WithMany(t => t.RoomBlocks)
            .HasForeignKey(x => new { x.HotelTaskId, x.RoomId })
            .HasPrincipalKey(t => new { t.Id, t.RoomId });

        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId);

        b.HasIndex(x => new { x.RoomId, x.StartAt });
    }
}