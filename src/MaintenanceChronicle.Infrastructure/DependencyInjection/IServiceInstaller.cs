using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Infrastructure.DependencyInjection;

public interface IServiceInstaller
{
    int Order { get; }
    void Install(IServiceCollection services, IConfiguration configuration);
}