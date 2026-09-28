using System.Linq.Expressions;

namespace MaintenanceChronicle.Infrastructure.Persistence;

/// <summary>
/// Provides read operations for entities.
/// </summary>
/// <typeparam name="TEntity">Type of entity returned by the repository.</typeparam>
public interface IReadOnlyRepository<TEntity>
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">ID of the entity to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <returns>The entity, or null when it is not found.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one entity using a specification.
    /// </summary>
    /// <param name="specification">Specification to apply to the entity query.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <returns>The matching entity, or null when no entity matches.</returns>
    Task<TEntity?> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities using a specification.
    /// </summary>
    /// <param name="specification">Specification to apply to the entity query.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyList<TEntity>> ListBySpecificationAsync(IListSpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities with the requested related data.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <param name="includes">Related properties to include in the result.</param>
    /// <returns>The entities in the repository.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Lists a page of entities with the requested related data.
    /// </summary>
    /// <param name="skip">Number of entities to skip.</param>
    /// <param name="take">Maximum number of entities to return.</param>
    /// <param name="cancellationToken">Token to cancel the query.</param>
    /// <param name="includes">Related properties to include in the result.</param>
    /// <returns>The requested page of entities.</returns>
    Task<IReadOnlyList<TEntity>> ListPageAsync(int skip, int take, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);

}
