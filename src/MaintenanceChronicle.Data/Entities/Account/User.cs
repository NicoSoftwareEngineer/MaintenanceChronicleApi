using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Account;

[Table(nameof(User))]
public class User : IdentityUser<Guid>, ITenant, ITrackable, IHasId
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserRole> Roles { get; set; } = new HashSet<UserRole>();
    public ICollection<LocationContactUser> Locations { get; set; } = new HashSet<LocationContactUser>();
    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = null!;
    public Instant? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
