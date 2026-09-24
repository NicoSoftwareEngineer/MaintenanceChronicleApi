using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class LocationWithContactsSpecification(Guid locationId) : ISpecification<Location>
{
    public async Task<Location?> ApplyAsync(IQueryable<Location> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(location => location.Contacts)
            .FirstOrDefaultAsync(location => location.Id == locationId, cancellationToken);
    }
}
