using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Commands;
/// <summary>
/// Handler for <see cref="MarkMaintenanceReminderAsSentCommand"/>
/// </summary>
public class MarkMaintenanceReminderAsSentCommandHandler(IRepository<MaintenanceReminder> reminderRepository, IUnitOfWork uow) : IRequestHandler<MarkMaintenanceReminderAsSentCommand>
{
    public async Task Handle(MarkMaintenanceReminderAsSentCommand request, CancellationToken cancellationToken)
    {
        // Find the reminder in the database
        var maintenanceReminder = await reminderRepository.GetByIdAsync(request.ReminderId, cancellationToken);

        // If the reminder is found, mark it as sent
        if (maintenanceReminder is not null)
        {
            maintenanceReminder.WasReminderSent = true;
            await uow.SaveChangesAsync(cancellationToken);
        }
    }
}
