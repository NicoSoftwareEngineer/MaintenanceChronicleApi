using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class LocationContactsForLocationSpecification(Guid locationId) : IListSpecification<LocationContactUser>
{
    public async Task<IReadOnlyList<LocationContactUser>> ApplyAsync(IQueryable<LocationContactUser> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(contact => contact.User)
            .Where(contact => contact.LocationId == locationId)
            .ToListAsync(cancellationToken);
    }
}
