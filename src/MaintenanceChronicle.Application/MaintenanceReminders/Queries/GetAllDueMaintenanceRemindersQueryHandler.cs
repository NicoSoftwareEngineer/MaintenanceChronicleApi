using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Queries;
/// <summary>
/// Handler for <see cref="GetAllDueMaintenanceRemindersQuery"/>.
/// </summary>
public class GetAllDueMaintenanceRemindersQueryHandler(IReadOnlyRepository<MaintenanceReminder> reminderReadOnlyRepository, IClock clock) : IRequestHandler<GetAllDueMaintenanceRemindersQuery, List<DueMaintenanceReminderDto>>
{
    public async Task<List<DueMaintenanceReminderDto>> Handle(GetAllDueMaintenanceRemindersQuery request, CancellationToken cancellationToken)
    {
        var specification = new DueMaintenanceRemindersSpecification(clock.GetCurrentInstant());
        var dueMaintenanceReminders = await reminderReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return dueMaintenanceReminders.Select(reminder => reminder.ToDueDto()).ToList();
    }
}
