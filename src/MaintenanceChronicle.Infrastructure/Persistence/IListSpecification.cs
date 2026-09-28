namespace MaintenanceChronicle.Infrastructure.Persistence;

/// <summary>
/// Defines a query that returns a list of entities.
/// </summary>
/// <typeparam name="TEntity">Type of entity returned by the query.</typeparam>
public interface IListSpecification<TEntity>
{
    /// <summary>
    /// Applies the specification to the supplied query.
    /// </summary>
    /// <param name="queryable">Entity query to apply the specification to.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyList<TEntity>> ApplyAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
}
