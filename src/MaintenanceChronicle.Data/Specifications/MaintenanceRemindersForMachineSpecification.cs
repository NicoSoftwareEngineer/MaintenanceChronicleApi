using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class MaintenanceRemindersForMachineSpecification(Guid machineId) : IListSpecification<MaintenanceReminder>
{
    public async Task<IReadOnlyList<MaintenanceReminder>> ApplyAsync(IQueryable<MaintenanceReminder> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(reminder => reminder.Machine)
            .Where(reminder => reminder.MachineId == machineId)
            .ToListAsync(cancellationToken);
    }
}
