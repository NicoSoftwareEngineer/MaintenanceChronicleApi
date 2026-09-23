using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;

namespace MaintenanceChronicle.Data.Repositories;

public class GenericRepository<TEntity>(AppDbContext dbContext) : GenericReadOnlyRepository<TEntity>(dbContext), IRepository<TEntity> where TEntity : class, IHasId
{
    public override async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);

        return entity;
    }

    public async Task AddAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default)
    { 
        await DbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entity;
    }
}