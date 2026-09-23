using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Utilities.Constants;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Business;

[Table(nameof(Customer))]
public class Customer : ITrackable, ITenant, IHasId
{
    public Guid Id { get; set; }
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Name { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxPhoneNumberLength)]
    public string PhoneNumber { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string CompanyIdNumber { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxEmailLength)]
    public string Email { get; set; } = null!;
    public ICollection<Location> Locations { get; set; } = new HashSet<Location>();
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
