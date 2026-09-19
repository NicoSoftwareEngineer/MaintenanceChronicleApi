using System.Security.Cryptography;

namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
{
    Task AddAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task ForceUpdateAsync(TEntity entity, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task DeleteRangeByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default, Guid? tenantId = null);
}