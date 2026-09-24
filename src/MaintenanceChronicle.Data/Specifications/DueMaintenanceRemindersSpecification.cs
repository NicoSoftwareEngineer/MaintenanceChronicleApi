using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MaintenanceChronicle.Data.Specifications;

public class DueMaintenanceRemindersSpecification(Instant currentInstant) : IListSpecification<MaintenanceReminder>
{
    public async Task<IReadOnlyList<MaintenanceReminder>> ApplyAsync(IQueryable<MaintenanceReminder> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(reminder => reminder.Machine)
            .Where(reminder => reminder.Date <= currentInstant)
            .ToListAsync(cancellationToken);
    }
}
