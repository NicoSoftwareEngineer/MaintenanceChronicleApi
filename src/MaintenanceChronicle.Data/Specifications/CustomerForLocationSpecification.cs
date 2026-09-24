using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class CustomerForLocationSpecification(Guid locationId) : ISpecification<Customer>
{
    public async Task<Customer?> ApplyAsync(IQueryable<Customer> queryable, CancellationToken cancellationToken = default)
    {
        var customer = await queryable
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(customer => customer.Locations
                                                 .Any(location => location.Id == locationId),
                cancellationToken);

        return customer;
    }
}