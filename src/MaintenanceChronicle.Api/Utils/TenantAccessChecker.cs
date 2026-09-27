using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;

namespace MaintenanceChronicle.Api.Utils;

public sealed class TenantAccessChecker(IHttpContextAccessor httpContextAccessor) : ITenantAccessChecker
{
    private bool _systemAccessEnabled;

    public TenantAccess Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            var tenantClaim = httpContext?.User.FindFirst(MaintenanceChronicleClaimTypes.TenantIdClaimType);

            if (tenantClaim is not null)
            {
                return Guid.TryParse(tenantClaim.Value, out var tenantId) && tenantId != Guid.Empty
                    ? new TenantAccess(TenantAccessMode.Tenant, tenantId)
                    : new TenantAccess(TenantAccessMode.Denied, null);
            }

            if (_systemAccessEnabled)
            {
                return new TenantAccess(TenantAccessMode.System, null);
            }

            return httpContext?.GetEndpoint()?.Metadata.GetMetadata<AllowTenantlessDataAccessAttribute>() is not null
                ? new TenantAccess(TenantAccessMode.Public, null)
                : new TenantAccess(TenantAccessMode.Denied, null);
        }
    }

    internal void EnableSystemAccess() => _systemAccessEnabled = true;
}
