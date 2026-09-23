using MaintenanceChronicle.Infrastructure.Persistence;

namespace MaintenanceChronicle.Data.Repositories;

public class GenericRepository<TEntity>(AppDbContext dbContext) : GenericReadOnlyRepository<TEntity>(dbContext), IRepository<TEntity> where TEntity : class
{
    private readonly AppDbContext _dbContext = dbContext;

    public override async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

        return entity;
    }

    public async Task AddAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default)
    { 
        await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        return entity;
    }
}