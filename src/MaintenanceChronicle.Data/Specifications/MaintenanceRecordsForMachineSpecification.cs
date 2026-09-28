using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class MaintenanceRecordsForMachineSpecification(Guid machineId) : IListSpecification<MaintenanceRecord>
{
    public async Task<IReadOnlyList<MaintenanceRecord>> ApplyAsync(IQueryable<MaintenanceRecord> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Where(record => record.MachineId == machineId)
            .ToListAsync(cancellationToken);
    }
}
