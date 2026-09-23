using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Utilities.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NodaTime;

namespace MaintenanceChronicle.Data.Entities.Business;

[Table(nameof(MaintenanceReminder))]
public class MaintenanceReminder : ITenant, ITrackable, IHasId
{
    public Guid Id { get; set; }
    [MaxLength(StringLengthConstants.MaxDescriptionLength)]
    public string Description { get; set; } = null!;
    public Instant Date { get; set; }
    public Guid MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public bool WasReminderSent { get; set; } = false;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Instant CreatedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string CreatedBy { get; set; } = null!;
    public Instant ModifiedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string ModifiedBy { get; set; } = null!;
    public Instant? DeletedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxIdLength)]
    public string? DeletedBy { get; set; }
}
