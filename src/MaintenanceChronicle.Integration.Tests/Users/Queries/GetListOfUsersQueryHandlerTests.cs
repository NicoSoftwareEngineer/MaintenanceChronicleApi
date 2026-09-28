using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application.Contracts.Users.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Users.Queries;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Integration.Tests.Users.Queries;

public sealed class GetListOfUsersQueryHandlerTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Handle_ReturnsUsersInIdOrderWithOneExtraItemForNextPage()
    {
        // Arrange
        var requestContext = new DefaultHttpContext();
        requestContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowTenantlessDataAccessAttribute()),
            "users-test"));
        var checker = new TenantAccessChecker(new HttpContextAccessor { HttpContext = requestContext });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => fixture.CreateContext(checker));
        services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<AppDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var handler = new GetListOfUsersQueryHandler(scope.ServiceProvider.GetRequiredService<UserManager<User>>());
        var expectedIds = new[] { fixture.First.UserId, fixture.Second.UserId }.Order().ToArray();

        // Act
        var firstPage = await handler.Handle(
            new GetListOfEntityQuery<UserListDto>(new PageRequest(1, 1)), CancellationToken.None);
        var secondPage = await handler.Handle(
            new GetListOfEntityQuery<UserListDto>(new PageRequest(2, 1)), CancellationToken.None);

        // Assert
        Assert.Equal(expectedIds, firstPage.Select(user => user.Id));
        Assert.Equal(expectedIds[1], Assert.Single(secondPage).Id);
        Assert.All(firstPage, user => Assert.Equal("Technician", Assert.Single(user.Roles).Name));
    }
}
