using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Commands;
/// <summary>
/// Handler for <see cref="UpdateMaintenanceReminderCommand"/>
/// </summary>
public class UpdateMaintenanceReminderCommandHandler(IRepository<MaintenanceReminder> reminderRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<UpdateMaintenanceReminderCommand, MaintenanceReminderDetailDto>
{
    public async Task<MaintenanceReminderDetailDto> Handle(UpdateMaintenanceReminderCommand request, CancellationToken cancellationToken)
    {
        // Get current reminder from db
        var maintenanceReminder = await reminderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (maintenanceReminder == null)
        {
            throw new BadRequestException(ErrorType.MaintenanceReminderNotFound);
        }

        // Apply patch to dto and map back to entity
        var dto = maintenanceReminder.ToDto();
        request.Patch.ApplyTo(dto);
        dto.MapToEntity(maintenanceReminder);

        maintenanceReminder.SetModifyBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);
        return dto;
    }
}
