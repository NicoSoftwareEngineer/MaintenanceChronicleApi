namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IReadOnlyRepository<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

}