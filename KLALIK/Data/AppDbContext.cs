using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CollectionDirection> CollectionDirections => Set<CollectionDirection>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<WorkshopService> WorkshopServices => Set<WorkshopService>();
    public DbSet<QualificationLevel> QualificationLevels => Set<QualificationLevel>();
    public DbSet<MasterProfile> MasterProfiles => Set<MasterProfile>();
    public DbSet<MasterServiceLink> MasterServiceLinks => Set<MasterServiceLink>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<BalanceTransaction> BalanceTransactions => Set<BalanceTransaction>();
    public DbSet<QualificationRequest> QualificationRequests => Set<QualificationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Balance).HasPrecision(12, 2);
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
            e.HasOne(u => u.MasterProfile).WithOne(m => m.User).HasForeignKey<MasterProfile>(m => m.UserId);
        });

        modelBuilder.Entity<CollectionDirection>(e => e.HasIndex(d => d.Name).IsUnique());
        modelBuilder.Entity<ServiceCategory>(e => e.HasIndex(c => c.Name).IsUnique());
        modelBuilder.Entity<QualificationLevel>(e => e.HasIndex(l => l.SortOrder));

        modelBuilder.Entity<WorkshopService>(e =>
        {
            e.Property(s => s.Price).HasPrecision(12, 2);
            e.HasOne(s => s.CollectionDirection).WithMany(d => d.Services).HasForeignKey(s => s.CollectionDirectionId);
            e.HasOne(s => s.ServiceCategory).WithMany(c => c.Services).HasForeignKey(s => s.ServiceCategoryId);
        });

        modelBuilder.Entity<MasterProfile>(e =>
        {
            e.HasIndex(m => m.UserId).IsUnique();
            e.HasOne(m => m.QualificationLevel).WithMany(l => l.MasterProfiles).HasForeignKey(m => m.QualificationLevelId);
        });

        modelBuilder.Entity<MasterServiceLink>(e =>
        {
            e.HasIndex(l => new { l.MasterProfileId, l.WorkshopServiceId }).IsUnique();
            e.HasOne(l => l.MasterProfile).WithMany(m => m.ServiceLinks).HasForeignKey(l => l.MasterProfileId);
            e.HasOne(l => l.WorkshopService).WithMany(s => s.MasterLinks).HasForeignKey(l => l.WorkshopServiceId);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Client).WithMany(u => u.ClientBookings).HasForeignKey(b => b.ClientUserId);
            e.HasOne(b => b.MasterProfile).WithMany(m => m.Bookings).HasForeignKey(b => b.MasterProfileId);
            e.HasOne(b => b.WorkshopService).WithMany(s => s.Bookings).HasForeignKey(b => b.WorkshopServiceId);
            e.Property(b => b.Status).HasConversion<int>();
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.HasOne(r => r.Client).WithMany(u => u.Reviews).HasForeignKey(r => r.ClientUserId);
            e.HasOne(r => r.WorkshopService).WithMany(s => s.Reviews).HasForeignKey(r => r.WorkshopServiceId);
            e.HasOne(r => r.MasterProfile).WithMany(m => m.Reviews).HasForeignKey(r => r.MasterProfileId);
            e.HasCheckConstraint("ck_review_target", "\"workshop_service_id\" IS NOT NULL OR \"master_profile_id\" IS NOT NULL");
        });

        modelBuilder.Entity<BalanceTransaction>(e =>
        {
            e.Property(t => t.Amount).HasPrecision(12, 2);
            e.HasOne(t => t.User).WithMany(u => u.BalanceTransactions).HasForeignKey(t => t.UserId);
            e.Property(t => t.TransactionType).HasConversion<int>();
        });

        modelBuilder.Entity<QualificationRequest>(e =>
        {
            e.HasOne(q => q.MasterProfile).WithMany(m => m.QualificationRequests).HasForeignKey(q => q.MasterProfileId);
            e.HasOne(q => q.Resolver).WithMany(u => u.ResolvedQualificationRequests).HasForeignKey(q => q.ResolverUserId);
            e.Property(q => q.Status).HasConversion<int>();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(ChangeTracker, DateTime.UtcNow);
        return base.SaveChangesAsync(cancellationToken);
    }
}