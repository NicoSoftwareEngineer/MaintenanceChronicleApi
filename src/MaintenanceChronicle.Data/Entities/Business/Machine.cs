using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Utilities.Constants;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Business;

[Table(nameof(Machine))]
public class Machine : ITrackable, ITenant, IHasId
{
    public Guid Id { get; set; }
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Model { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Manufacture { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxSerialNumberLength)]
    public string SerialNumber { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Color { get; set; } = null!;
    public Instant InUseSince { get; set; }
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
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new HashSet<MaintenanceRecord>();
}
