using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class MachineWithLocationContactsSpecification(Guid machineId) : ISpecification<Machine>
{
    public async Task<Machine?> ApplyAsync(IQueryable<Machine> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(machine => machine.Location)
                .ThenInclude(location => location.Contacts)
                    .ThenInclude(contact => contact.User)
            .FirstOrDefaultAsync(machine => machine.Id == machineId, cancellationToken);
    }
}
