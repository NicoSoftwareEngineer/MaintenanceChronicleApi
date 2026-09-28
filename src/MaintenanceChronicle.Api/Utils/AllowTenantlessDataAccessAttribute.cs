namespace MaintenanceChronicle.Api.Utils;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowTenantlessDataAccessAttribute : Attribute
{
}
