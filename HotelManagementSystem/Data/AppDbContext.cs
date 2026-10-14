using HotelManagementSystem.Entities;
using HotelManagementSystem.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<StayGuest> StayGuests => Set<StayGuest>();
    public DbSet<HotelService> HotelServices => Set<HotelService>();
    public DbSet<ServiceUsage> ServiceUsages => Set<ServiceUsage>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<HotelTask> HotelTasks => Set<HotelTask>();
    public DbSet<RoomBlock> RoomBlocks => Set<RoomBlock>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Nạp tất cả file cấu hình trong thư mục Data/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Không cascade delete để bảo toàn dữ liệu lịch sử
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Tự gán CreatedAt / UpdatedAt (UTC)
    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = now;
        }
    }
}