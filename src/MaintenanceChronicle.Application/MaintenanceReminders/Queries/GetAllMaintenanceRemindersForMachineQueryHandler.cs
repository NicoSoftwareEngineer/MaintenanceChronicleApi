using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Queries;
/// <summary>
/// Handler for <see cref="GetAllMaintenanceRemindersForMachineQuery"/>.
/// </summary>
public class GetAllMaintenanceRemindersForMachineQueryHandler(IReadOnlyRepository<MaintenanceReminder> reminderReadOnlyRepository) : IRequestHandler<GetAllMaintenanceRemindersForMachineQuery, List<MaintenanceReminderInListForMachineDto>>
{
    public async Task<List<MaintenanceReminderInListForMachineDto>> Handle(GetAllMaintenanceRemindersForMachineQuery request, CancellationToken cancellationToken)
    {
        var specification = new MaintenanceRemindersForMachineSpecification(request.MachineId);
        var maintenanceReminders = await reminderReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return maintenanceReminders.Select(reminder => reminder.ToListDto()).ToList();
    }
}
