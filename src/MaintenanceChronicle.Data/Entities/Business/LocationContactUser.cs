using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Utilities.Constants;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Business;
[Table(nameof(LocationContactUser))]
public class LocationContactUser : ITrackable, ITenant, IHasId
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public Instant CreatedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string CreatedBy { get; set; } = null!;
    public Instant ModifiedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string ModifiedBy { get; set; } = null!;
    public Instant? DeletedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string? DeletedBy { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
