using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.BackgroundServices;

public interface ISystemTenantScopeFactory
{
    IServiceScope CreateScope();
}
