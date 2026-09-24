namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IListSpecification<TEntity>
{
    Task<IReadOnlyList<TEntity>> ApplyAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
}
