using System.Security.Claims;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.BackgroundServices;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Repositories;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Integration.Tests;

public sealed class TenantSegregationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task TenantClaimLimitsEveryTenantFilteredEntity()
    {
        // Arrange
        var checker = CreateChecker(fixture.First.TenantId.ToString());
        await using var db = fixture.CreateContext(checker);

        // Act
        var visibleRows = await ReadVisibleRowsAsync(db);

        // Assert
        Assert.Equal(TenantAccessMode.Tenant, checker.Current.Mode);
        AssertVisibleRows(visibleRows, fixture.First);
    }

    [Fact]
    public async Task AnotherTenantClaimSeesOnlyItsOwnRows()
    {
        // Arrange
        var checker = CreateChecker(fixture.Second.TenantId.ToString());
        await using var db = fixture.CreateContext(checker);

        // Act
        var visibleRows = await ReadVisibleRowsAsync(db);

        // Assert
        Assert.Equal(TenantAccessMode.Tenant, checker.Current.Mode);
        AssertVisibleRows(visibleRows, fixture.Second);
    }

    [Fact]
    public async Task TenantClaimTakesPriorityOnPublicEndpoint()
    {
        // Arrange
        var checker = CreateChecker(fixture.First.TenantId.ToString(), publicEndpoint: true);
        await using var db = fixture.CreateContext(checker);
        var contacts = new GetListOfContactsForLocationQueryHandler(
            new GenericReadOnlyRepository<LocationContactUser>(db));

        // Act
        var visibleRows = await ReadVisibleRowsAsync(db);
        var otherTenantContacts = await contacts.Handle(
            new GetListOfContactsForLocationQuery(fixture.Second.LocationId), CancellationToken.None);

        // Assert
        Assert.Equal(TenantAccessMode.Tenant, checker.Current.Mode);
        AssertVisibleRows(visibleRows, fixture.First);
        Assert.Empty(otherTenantContacts);
    }

    [Fact]
    public async Task MissingTenantAndUnmarkedEndpointDenyAccess()
    {
        // Arrange
        var checker = CreateChecker();
        await using var db = fixture.CreateContext(checker);

        // Act
        var visibleRows = await ReadVisibleRowsAsync(db);

        // Assert
        Assert.Equal(TenantAccessMode.Denied, checker.Current.Mode);
        AssertVisibleRows(visibleRows);
    }

    [Fact]
    public async Task InvalidTenantClaimDeniesAccessEvenOnPublicEndpoint()
    {
        // Arrange
        var checker = CreateChecker("invalid", publicEndpoint: true);
        await using var db = fixture.CreateContext(checker);

        // Act
        var machines = await db.Machines.ToListAsync();
        var contacts = await db.LocationContactUsers.ToListAsync();

        // Assert
        Assert.Equal(TenantAccessMode.Denied, checker.Current.Mode);
        Assert.Empty(machines);
        Assert.Empty(contacts);
    }

    [Fact]
    public async Task PublicEndpointFindsContactsForScannedMachine()
    {
        // Arrange
        var checker = CreateChecker(publicEndpoint: true);
        await using var db = fixture.CreateContext(checker);
        var machineRepository = new GenericReadOnlyRepository<Machine>(db);
        var locationHandler = new GetLocationForMachineQueryHandler(
            new GenericReadOnlyRepository<Location>(db));
        var contactsHandler = new GetListOfContactsForLocationQueryHandler(
            new GenericReadOnlyRepository<LocationContactUser>(db));

        // Act
        var visibleRows = await ReadVisibleRowsAsync(db);
        var machine = await machineRepository.GetByIdAsync(fixture.Second.MachineId);
        var location = await locationHandler.Handle(
            new GetLocationForMachineQuery(fixture.Second.MachineId), CancellationToken.None);
        var contacts = await contactsHandler.Handle(
            new GetListOfContactsForLocationQuery(fixture.Second.LocationId), CancellationToken.None);

        // Assert
        Assert.Equal(TenantAccessMode.Public, checker.Current.Mode);
        AssertVisibleRows(visibleRows, fixture.First, fixture.Second);
        Assert.NotNull(machine);
        Assert.Equal(fixture.Second.MachineId, machine.Id);
        Assert.Equal(fixture.Second.LocationId, location.Id);
        Assert.Equal(fixture.Second.UserId, Assert.Single(contacts).Id);
    }

    [Fact]
    public async Task SystemScopeReadsActiveRowsAcrossTenants()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddScoped<TenantAccessChecker>();
        services.AddScoped<ITenantAccessChecker>(provider => provider.GetRequiredService<TenantAccessChecker>());
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
            fixture.ConnectionString, npgsql =>
            {
                npgsql.UseNodaTime();
                npgsql.MapEnum<RecordType>("recordType");
            }));
        services.AddSingleton<ISystemTenantScopeFactory, SystemTenantScopeFactory>();

        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

        // Act
        using var scope = provider.GetRequiredService<ISystemTenantScopeFactory>().CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<ITenantAccessChecker>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var visibleRows = await ReadVisibleRowsAsync(db);

        // Assert
        Assert.Equal(TenantAccessMode.System, checker.Current.Mode);
        AssertVisibleRows(visibleRows, fixture.First, fixture.Second);
    }

    /// <summary>
    /// Creates a checker with a synthetic HTTP request for the selected tenant claim and endpoint mode.
    /// </summary>
    /// <param name="tenantClaim">Tenant claim value, or null to leave the claim absent.</param>
    /// <param name="publicEndpoint">Whether the endpoint permits tenantless data access.</param>
    /// <returns>A checker that resolves access from the synthetic request.</returns>
    private static TenantAccessChecker CreateChecker(string? tenantClaim = null, bool publicEndpoint = false)
    {
        var context = new DefaultHttpContext();
        if (tenantClaim is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(MaintenanceChronicleClaimTypes.TenantIdClaimType, tenantClaim)], "Test"));
        }

        if (publicEndpoint)
        {
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new AllowTenantlessDataAccessAttribute()),
                "public"));
        }

        return new TenantAccessChecker(new HttpContextAccessor { HttpContext = context });
    }

    /// <summary>
    /// Reads the IDs visible through every tenant-filtered entity set in the current context.
    /// </summary>
    /// <param name="db">Context whose tenant access mode determines row visibility.</param>
    /// <returns>The visible IDs grouped by entity type.</returns>
    private static async Task<VisibleRows> ReadVisibleRowsAsync(AppDbContext db)
        => new(
            await db.Tenants.Select(row => row.Id).ToArrayAsync(),
            await db.Customers.Select(row => row.Id).ToArrayAsync(),
            await db.Locations.Select(row => row.Id).ToArrayAsync(),
            await db.Machines.Select(row => row.Id).ToArrayAsync(),
            await db.Users.Select(row => row.Id).ToArrayAsync(),
            await db.LocationContactUsers.Select(row => row.Id).ToArrayAsync(),
            await db.MaintenanceRecords.Select(row => row.Id).ToArrayAsync(),
            await db.MaintenanceReminders.Select(row => row.Id).ToArrayAsync(),
            await db.UserRoles.Select(row => row.Id).ToArrayAsync());

    /// <summary>
    /// Checks that each filtered entity set contains only active rows from the expected tenants.
    /// </summary>
    /// <param name="actual">IDs returned by the filtered queries.</param>
    /// <param name="expected">Tenants whose active rows should be visible; empty when access is denied.</param>
    private static void AssertVisibleRows(VisibleRows actual, params SeededTenant[] expected)
    {
        AssertIds(expected.Select(row => row.TenantId), actual.Tenants);
        AssertIds(expected.Select(row => row.CustomerId), actual.Customers);
        AssertIds(expected.Select(row => row.LocationId), actual.Locations);
        AssertIds(expected.Select(row => row.MachineId), actual.Machines);
        AssertIds(expected.Select(row => row.UserId), actual.Users);
        AssertIds(expected.Select(row => row.ContactId), actual.Contacts);
        AssertIds(expected.Select(row => row.RecordId), actual.Records);
        AssertIds(expected.Select(row => row.ReminderId), actual.Reminders);
        AssertIds(expected.Select(row => row.UserRoleId), actual.UserRoles);
    }

    private static void AssertIds(IEnumerable<Guid> expected, IEnumerable<Guid> actual)
        => Assert.Equal(expected.Order(), actual.Order());

    private sealed record VisibleRows(
        Guid[] Tenants,
        Guid[] Customers,
        Guid[] Locations,
        Guid[] Machines,
        Guid[] Users,
        Guid[] Contacts,
        Guid[] Records,
        Guid[] Reminders,
        Guid[] UserRoles);
}
