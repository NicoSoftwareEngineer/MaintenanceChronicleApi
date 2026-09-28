using System.Linq.Expressions;

namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IReadOnlyRepository<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TEntity?> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListBySpecificationAsync(IListSpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);
    Task<IReadOnlyList<TEntity>> ListPageAsync(int skip, int take, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);

}
