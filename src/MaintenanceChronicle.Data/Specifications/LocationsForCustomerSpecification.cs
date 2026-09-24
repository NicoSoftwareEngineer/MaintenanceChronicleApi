using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class LocationsForCustomerSpecification(Guid customerId) : IListSpecification<Location>
{
    public async Task<IReadOnlyList<Location>> ApplyAsync(IQueryable<Location> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(location => location.Customer)
            .Where(location => location.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }
}
