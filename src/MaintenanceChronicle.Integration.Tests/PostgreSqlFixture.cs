using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Testcontainers.PostgreSql;

namespace MaintenanceChronicle.Integration.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private static readonly Guid TechnicianRoleId = Guid.Parse("078500E4-4917-4CB0-B6EF-AEEF745BCCA8");
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public SeededTenant First { get; private set; } = null!;
    public SeededTenant Second { get; private set; } = null!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateContext(new TenantAccessChecker(new HttpContextAccessor()));
        await db.Database.MigrateAsync();

        First = SeedTenant(db, "first");
        Second = SeedTenant(db, "second");
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AppDbContext CreateContext(TenantAccessChecker checker)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
            {
                npgsql.UseNodaTime();
                npgsql.MapEnum<RecordType>("recordType");
            })
            .Options;

        return new AppDbContext(options, checker);
    }

    /// <summary>
    /// Stages one tenant's related rows, including deleted machine and contact rows, for filter tests.
    /// </summary>
    /// <param name="db">Context tracking the rows; the caller saves them.</param>
    /// <param name="name">Distinctive value used for the tenant and related rows.</param>
    /// <returns>The IDs needed to check which rows each access mode can see.</returns>
    private static SeededTenant SeedTenant(AppDbContext db, string name)
    {
        var rows = new SeededTenant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        db.Tenants.Add(Track(new Tenant { Id = rows.TenantId, Name = name }));
        db.Customers.Add(Track(new Customer
        {
            Id = rows.CustomerId, TenantId = rows.TenantId, Name = name,
            PhoneNumber = "123456789", CompanyIdNumber = name, Email = $"{name}@example.test"
        }));
        db.Locations.Add(Track(new Location
        {
            Id = rows.LocationId, TenantId = rows.TenantId, CustomerId = rows.CustomerId,
            Name = name, Street = "Street", City = "City", Country = "Country"
        }));
        db.Machines.Add(Track(new Machine
        {
            Id = rows.MachineId, TenantId = rows.TenantId, LocationId = rows.LocationId,
            Model = name, Manufacture = "Maker", SerialNumber = name, Color = "Blue",
            InUseSince = SystemClock.Instance.GetCurrentInstant()
        }));
        db.Machines.Add(Track(new Machine
        {
            Id = rows.DeletedMachineId, TenantId = rows.TenantId, LocationId = rows.LocationId,
            Model = $"{name} deleted", Manufacture = "Maker", SerialNumber = $"{name} deleted",
            Color = "Blue", InUseSince = SystemClock.Instance.GetCurrentInstant(),
            DeletedAt = SystemClock.Instance.GetCurrentInstant()
        }));
        db.Users.Add(Track(new User
        {
            Id = rows.UserId, TenantId = rows.TenantId, FirstName = name, LastName = "Technician",
            UserName = name, NormalizedUserName = name.ToUpperInvariant(),
            Email = $"{name}@example.test", NormalizedEmail = $"{name.ToUpperInvariant()}@EXAMPLE.TEST"
        }));
        db.LocationContactUsers.Add(Track(new LocationContactUser
        {
            Id = rows.ContactId, TenantId = rows.TenantId, LocationId = rows.LocationId,
            UserId = rows.UserId
        }));
        db.LocationContactUsers.Add(Track(new LocationContactUser
        {
            Id = rows.DeletedContactId, TenantId = rows.TenantId, LocationId = rows.LocationId,
            UserId = rows.UserId, DeletedAt = SystemClock.Instance.GetCurrentInstant()
        }));
        db.MaintenanceRecords.Add(Track(new MaintenanceRecord
        {
            Id = rows.RecordId, TenantId = rows.TenantId, MachineId = rows.MachineId,
            Description = name, Date = SystemClock.Instance.GetCurrentInstant(), Type = RecordType.Maintenance
        }));
        db.MaintenanceReminders.Add(Track(new MaintenanceReminder
        {
            Id = rows.ReminderId, TenantId = rows.TenantId, MachineId = rows.MachineId,
            Description = name, Date = SystemClock.Instance.GetCurrentInstant()
        }));
        db.UserRoles.Add(Track(new UserRole
        {
            Id = rows.UserRoleId, TenantId = rows.TenantId, UserId = rows.UserId,
            RoleId = TechnicianRoleId
        }));

        return rows;
    }

    private static T Track<T>(T entity) where T : ITrackable
    {
        entity.CreatedAt = SystemClock.Instance.GetCurrentInstant();
        entity.ModifiedAt = entity.CreatedAt;
        entity.CreatedBy = "IntegrationTest";
        entity.ModifiedBy = "IntegrationTest";
        return entity;
    }
}

public sealed record SeededTenant(
    Guid TenantId,
    Guid CustomerId,
    Guid LocationId,
    Guid MachineId,
    Guid UserId,
    Guid ContactId,
    Guid RecordId,
    Guid ReminderId,
    Guid UserRoleId,
    Guid DeletedMachineId,
    Guid DeletedContactId);
