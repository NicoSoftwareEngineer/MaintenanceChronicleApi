using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class MachineForMaintenanceRecordSpecification(Guid recordId) : ISpecification<Machine>
{
    public async Task<Machine?> ApplyAsync(IQueryable<Machine> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(machine => machine.Location)
            .FirstOrDefaultAsync(machine => machine.MaintenanceRecords.Any(record => record.Id == recordId), cancellationToken);
    }
}
