namespace MaintenanceChronicle.Utilities.Helpers;

public enum TenantAccessMode
{
    Denied,
    Tenant,
    Public,
    System
}

public readonly record struct TenantAccess(TenantAccessMode Mode, Guid? TenantId);
