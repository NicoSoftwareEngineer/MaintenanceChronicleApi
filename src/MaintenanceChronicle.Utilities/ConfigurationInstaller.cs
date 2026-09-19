using MaintenanceChronicle.Infrastructure.DependencyInjection;
using MaintenanceChronicle.Utilities.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Utilities;

public class ConfigurationInstaller : IServiceInstaller
{
    public int Order => 0;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        //Configure JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));

        services.Configure<SmtpOptions>(configuration.GetSection(nameof(SmtpOptions)));

        //Environment options
        services.Configure<EnvironmentOptions>(configuration.GetSection(nameof(EnvironmentOptions)));
    }
}