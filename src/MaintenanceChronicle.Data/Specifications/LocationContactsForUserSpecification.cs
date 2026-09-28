using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Specifications;

public class LocationContactsForUserSpecification(Guid userId) : IListSpecification<LocationContactUser>
{
    public async Task<IReadOnlyList<LocationContactUser>> ApplyAsync(IQueryable<LocationContactUser> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable
            .Include(contact => contact.Location)
            .Where(contact => contact.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
