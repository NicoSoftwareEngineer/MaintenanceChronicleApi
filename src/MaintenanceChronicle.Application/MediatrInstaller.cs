using MaintenanceChronicle.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Application;

public class MediatrInstaller : IServiceInstaller
{
    public int Order => 4;
    /// <summary>
    /// Installs the services for MediatR
    /// </summary>
    /// <param name="services">Service collection to add services to</param>
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatrInstaller).Assembly);
        });
    }
}