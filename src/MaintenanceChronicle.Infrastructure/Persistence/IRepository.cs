using System.Security.Cryptography;

namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
{
    Task AddAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}