using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Integration.Tests;

public class UnitOfWorkScopeTests
{
    [Fact]
    public void SynchronousScope_DisposesAfterResolvingUnitOfWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(_ => new AppDbContext(
            new DbContextOptions<AppDbContext>(),
            new TenantAccessChecker(new HttpContextAccessor())));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        using var provider = services.BuildServiceProvider();

        // Act
        var exception = Record.Exception(() =>
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        });

        // Assert
        Assert.Null(exception);
    }
}
