using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class HotelTaskConfiguration : IEntityTypeConfiguration<HotelTask>
{
    public void Configure(EntityTypeBuilder<HotelTask> b)
    {
        b.ToTable("HotelTasks", t =>
        {
            t.HasCheckConstraint("CK_HotelTasks_TaskType", "[TaskType] IN (1,2)");
            t.HasCheckConstraint("CK_HotelTasks_Status", "[Status] IN (1,2,3,4,5)");
            t.HasCheckConstraint("CK_HotelTasks_Priority", "[Priority] IN (1,2,3,4)");

            // Housekeeping bắt buộc có HousekeepingKind (1 hoặc 2); Maintenance để NULL
            t.HasCheckConstraint("CK_HotelTasks_HousekeepingKind",
                "([TaskType] = 1 AND [HousekeepingKind] IN (1,2)) OR ([TaskType] = 2 AND [HousekeepingKind] IS NULL)");
        });
        b.HasKey(x => x.Id);

        // Dùng cho khóa ngoại ghép RoomBlocks(HotelTaskId, RoomId) -> HotelTasks(Id, RoomId)
        b.HasAlternateKey(x => new { x.Id, x.RoomId });

        b.Property(x => x.TaskCode).HasColumnType("varchar(30)").IsRequired();
        b.HasIndex(x => x.TaskCode).IsUnique();

        b.Property(x => x.TaskType).HasConversion<byte>();
        b.Property(x => x.HousekeepingKind).HasConversion<byte?>();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Priority).HasConversion<byte>();
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.DueAt).HasColumnType("datetime2(3)");
        b.Property(x => x.StartedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CompletedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CompletionNotes).HasMaxLength(2000);

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AssignedToAccountId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.CompletedByAccountId);

        b.HasIndex(x => new { x.AssignedToAccountId, x.Status, x.CreatedAt });
        b.HasIndex(x => new { x.RoomId, x.TaskType, x.Status });
    }
}