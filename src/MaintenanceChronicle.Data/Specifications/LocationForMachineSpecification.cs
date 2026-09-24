using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class LocationForMachineSpecification(Guid machineId) : ISpecification<Location>
{
    public async Task<Location?> ApplyAsync(IQueryable<Location> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(location => location.Customer)
            .FirstOrDefaultAsync(location => location.Machines.Any(machine => machine.Id == machineId), cancellationToken);
    }
}
