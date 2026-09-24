namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface ISpecification<T>
{
    public Task<T> ApplyAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default);
}