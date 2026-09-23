using MaintenanceChronicle.Data.Interfaces;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaintenanceChronicle.Utilities.Constants;

namespace MaintenanceChronicle.Data.Entities.Business;
[Table(nameof(EmailMessage))]
public class EmailMessage : IHasId
{
    public Guid Id { get; set; }

    [MaxLength(StringLengthConstants.MaxEmailLength)]
    public Dictionary<string, string?> Recipients { get; set; } = new Dictionary<string, string?>();

    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string Subject { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxEmailBodyLength)]
    public string Body { get; set; } = null!;
    public bool Sent { get; set; }
    public Instant CreatedAt { get; set; }
    [MaxLength(StringLengthConstants.MaxEmailLength)]
    public string FromEmail { get; set; } = null!;
    [MaxLength(StringLengthConstants.MaxNameLength)]
    public string FromName { get; set; } = null!;
}
