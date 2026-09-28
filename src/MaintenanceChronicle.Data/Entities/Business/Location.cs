using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Utilities.Constants;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Business;

[Table(nameof(Location))]
public class Location : ITrackable, ITenant, IHasId
{
    public Guid Id { get; set; }
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Name { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxAddressLength)]
    public string Street { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxAddressLength)]
    public string City { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxAddressLength)]
    public string Country { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<LocationContactUser> Contacts { get; set; } = new HashSet<LocationContactUser>();
    public ICollection<Machine> Machines { get; set; } = new HashSet<Machine>();
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
