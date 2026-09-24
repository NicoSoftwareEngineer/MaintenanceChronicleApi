using System.Linq.Expressions;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Data.Repositories;

public class GenericReadOnlyRepository<TEntity>(AppDbContext dbContext) : IReadOnlyRepository<TEntity> where TEntity : class, IHasId
{
    protected AppDbContext DbContext { get; } = dbContext;

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return entity;
    }

    public async Task<TEntity> GetBySpecificationAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await specification.ApplyAsync(DbContext.Set<TEntity>(), cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> entities = DbContext.Set<TEntity>();

        foreach (var include in includes)
        {
            entities = entities.Include(include);
        }

        return await entities.AsNoTracking().ToListAsync(cancellationToken);
    }
}