namespace MaintenanceChronicle.Utilities.Helpers;

public interface ITenantAccessChecker
{
    TenantAccess Current { get; }
}
