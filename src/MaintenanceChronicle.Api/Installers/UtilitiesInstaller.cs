using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using MaintenanceChronicle.Utilities.Helpers;
using NodaTime;

namespace MaintenanceChronicle.Api.Installers;

public class UtilitiesInstaller : IServiceInstaller
{
    public int Order => 1;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        //These services are needed for the ICurrentTenantProvider
        services.AddHttpContextAccessor();
        services.AddDataProtection();

        //Method for global filter into db
        services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();

        //Clock
        services.AddSingleton<IClock>(SystemClock.Instance);
    }
}