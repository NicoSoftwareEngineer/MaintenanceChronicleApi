using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using MaintenanceChronicle.BackgroundServices;
using MaintenanceChronicle.Utilities.Helpers;
using NodaTime;

namespace MaintenanceChronicle.Api.Installers;

public class UtilitiesInstaller : IServiceInstaller
{
    public int Order => 1;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        //These services are needed for tenant access resolution
        services.AddHttpContextAccessor();
        services.AddDataProtection();

        services.AddScoped<TenantAccessChecker>();
        services.AddScoped<ITenantAccessChecker>(provider => provider.GetRequiredService<TenantAccessChecker>());
        services.AddSingleton<ISystemTenantScopeFactory, SystemTenantScopeFactory>();

        //Clock
        services.AddSingleton<IClock>(SystemClock.Instance);
    }
}