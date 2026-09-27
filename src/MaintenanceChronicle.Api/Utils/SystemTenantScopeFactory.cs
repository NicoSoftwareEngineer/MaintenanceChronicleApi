using MaintenanceChronicle.BackgroundServices;

namespace MaintenanceChronicle.Api.Utils;

public sealed class SystemTenantScopeFactory(IServiceScopeFactory scopeFactory) : ISystemTenantScopeFactory
{
    public IServiceScope CreateScope()
    {
        var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantAccessChecker>().EnableSystemAccess();
        return scope;
    }
}
