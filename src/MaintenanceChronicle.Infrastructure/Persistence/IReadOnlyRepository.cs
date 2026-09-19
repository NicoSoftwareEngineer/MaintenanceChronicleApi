namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IReadOnlyRepository<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task<long> LongCountAsync(CancellationToken cancellationToken = default, Guid? tenantId = null);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default, Guid? tenantId = null);

}