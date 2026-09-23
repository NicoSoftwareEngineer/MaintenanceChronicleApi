using MaintenanceChronicle.Infrastructure.Persistence;

namespace MaintenanceChronicle.Data;

public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private bool _isDisposed = false;
    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        await dbContext.DisposeAsync();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}