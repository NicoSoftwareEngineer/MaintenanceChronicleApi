namespace MaintenanceChronicle.Infrastructure.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}