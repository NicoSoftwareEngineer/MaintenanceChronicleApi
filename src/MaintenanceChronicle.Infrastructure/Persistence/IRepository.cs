using System.Security.Cryptography;

namespace MaintenanceChronicle.Infrastructure.Persistence;

/// <summary>
/// Provides entity retrieval and addition operations for write workflows.
/// </summary>
/// <typeparam name="TEntity">Type of entity stored by the repository.</typeparam>
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
{
    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    /// <param name="cancellationToken">Token to cancel the add operation.</param>
    /// <returns>A task that completes when the entities have been added.</returns>
    Task AddAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an entity to the repository.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the add operation.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}