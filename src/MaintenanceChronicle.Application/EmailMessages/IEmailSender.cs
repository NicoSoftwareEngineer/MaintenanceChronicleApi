using MaintenanceChronicle.Data.Entities.Business;

namespace MaintenanceChronicle.Application.EmailMessages;

public interface IEmailSender
{
    Task SendAsync(EmailMessage emailMessage, CancellationToken cancellationToken);
}
