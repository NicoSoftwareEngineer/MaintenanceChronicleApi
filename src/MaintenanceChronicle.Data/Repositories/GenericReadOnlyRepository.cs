using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Repositories;

public class GenericReadOnlyRepository<TEntity>(AppDbContext dbContext) : IReadOnlyRepository<TEntity> where TEntity : class
{
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

        return entity;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entity = dbContext.Set<TEntity>().AsQueryable();

        return await entity.ToListAsync(cancellationToken);
    }
}