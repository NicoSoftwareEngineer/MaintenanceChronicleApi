using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Commands;
/// <summary>
/// Handler for <see cref="CreateNewMaintenanceReminderCommand"/>
/// </summary>
public class CreateNewMaintenanceReminderCommandHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository, IRepository<MaintenanceReminder> reminderRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<CreateNewMaintenanceReminderCommand>
{
    public async Task Handle(CreateNewMaintenanceReminderCommand request, CancellationToken cancellationToken)
    {
        // Check if machine exists
        var machine = await machineReadOnlyRepository.GetByIdAsync(request.Reminder.MachineId, cancellationToken);
        if (machine == null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        // Map reminder to entity
        var reminderEntity = request.Reminder.ToEntity();
        reminderEntity.TenantId = Guid.Parse(request.TenantId);
        reminderEntity.SetCreateBy(request.UserId, clock.GetCurrentInstant());

        // Add reminder to database
        await reminderRepository.AddAsync(reminderEntity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
