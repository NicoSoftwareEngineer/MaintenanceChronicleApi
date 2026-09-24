using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class MachinesForLocationSpecification(Guid locationId) : IListSpecification<Machine>
{
    public async Task<IReadOnlyList<Machine>> ApplyAsync(IQueryable<Machine> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Where(machine => machine.LocationId == locationId)
            .ToListAsync(cancellationToken);
    }
}
