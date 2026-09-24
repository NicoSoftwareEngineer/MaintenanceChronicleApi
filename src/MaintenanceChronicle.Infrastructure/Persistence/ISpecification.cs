namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface ISpecification<TEntity>
{
    public Task<TEntity?> ApplyAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
}