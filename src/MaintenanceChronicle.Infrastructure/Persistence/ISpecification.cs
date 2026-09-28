namespace MaintenanceChronicle.Infrastructure.Persistence;

/// <summary>
/// Defines a query that returns one entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity returned by the query.</typeparam>
public interface ISpecification<TEntity>
{
    /// <summary>
    /// Applies the specification to the supplied query.
    /// </summary>
    /// <param name="queryable">Entity query to apply the specification to.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <returns>The matching entity, or null when no entity matches.</returns>
    public Task<TEntity?> ApplyAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
}