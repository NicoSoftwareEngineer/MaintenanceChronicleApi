using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantAccessChecker tenantAccessChecker)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    private Guid? CurrentTenantId => tenantAccessChecker.Current.TenantId;
    private bool CanReadAcrossTenants => tenantAccessChecker.Current.Mode is TenantAccessMode.Public or TenantAccessMode.System;

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<LocationContactUser> LocationContactUsers { get; set; } = null!;
    public DbSet<Machine> Machines { get; set; } = null!;
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; } = null!;
    public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; } = null!;
    public DbSet<EmailMessage> EmailMessages { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Global filters
        modelBuilder.Entity<User>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<UserRole>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<Tenant>().HasQueryFilter(b => (CanReadAcrossTenants || b.Id == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<Customer>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<Location>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<LocationContactUser>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<Machine>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<MaintenanceRecord>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);
        modelBuilder.Entity<MaintenanceReminder>().HasQueryFilter(b => (CanReadAcrossTenants || b.TenantId == CurrentTenantId) && b.DeletedAt == null);

        modelBuilder.Entity<EmailMessage>()
            .Property(e => e.Recipients)
            .HasColumnType("hstore");

        modelBuilder.Entity<UserRole>()
            .HasOne(e => e.Role)
            .WithMany(e => e.Users)
            .HasForeignKey(e => e.RoleId)
            .HasPrincipalKey(e => e.Id);
        modelBuilder.Entity<UserRole>()
            .HasOne(e => e.User)
            .WithMany(e => e.Roles)
            .HasForeignKey(e => e.UserId)
            .HasPrincipalKey(e => e.Id);
        modelBuilder.Entity<UserRole>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<Role>().HasData(new()
        {
            Id = Guid.Parse("FCD43167-B480-47D9-8A84-7B52C06E1EBB"),
            Name = RoleTypes.GlobalAdmin,
            NormalizedName = RoleTypes.GlobalAdmin.NormalizeToUpper(),
            ConcurrencyStamp = "26AB60F1-DACF-4CF1-996E-DB6E36E36CA5"
        }, new()
        {
            Id = Guid.Parse("3CD34831-A874-47B3-9AB0-5AFD29EDB69E"),
            Name = RoleTypes.Admin,
            NormalizedName = RoleTypes.Admin.NormalizeToUpper(),
            ConcurrencyStamp = "8D8BEBDE-9E70-4AD6-A3E8-34EDD0F7E0CE"
        }, new()
        {
            Id = Guid.Parse("078500E4-4917-4CB0-B6EF-AEEF745BCCA8"),
            Name = RoleTypes.Technician,
            NormalizedName = RoleTypes.Technician.NormalizeToUpper(),
            ConcurrencyStamp = "F5C8757B-9165-4DAA-9076-4CEBD2BD9416"
        });
    }
}
