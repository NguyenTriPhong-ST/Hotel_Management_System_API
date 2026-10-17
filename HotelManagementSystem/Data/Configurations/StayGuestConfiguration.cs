using HotelManagementSystem.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelManagementSystem.Data.Configurations;

public class StayGuestConfiguration : IEntityTypeConfiguration<StayGuest>
{
    public void Configure(EntityTypeBuilder<StayGuest> b)
    {
        b.ToTable("StayGuests");

        // Khóa chính ghép
        b.HasKey(x => new { x.StayId, x.GuestId });

        b.Property(x => x.AddedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Stay).WithMany(s => s.StayGuests).HasForeignKey(x => x.StayId);
        b.HasOne(x => x.Guest).WithMany(g => g.StayGuests).HasForeignKey(x => x.GuestId);

        // Mỗi stay có tối đa một khách đại diện
        b.HasIndex(x => x.StayId)
            .IsUnique()
            .HasFilter("[IsMainGuest] = 1")
            .HasDatabaseName("UX_StayGuests_MainGuest");

        // Truy vấn lịch sử lưu trú của khách
        b.HasIndex(x => new { x.GuestId, x.StayId });
    }
}