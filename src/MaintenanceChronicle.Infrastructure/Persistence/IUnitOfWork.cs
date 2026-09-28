namespace MaintenanceChronicle.Infrastructure.Persistence;

/// <summary>
/// Commits pending persistence changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves pending changes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>A task that completes when the changes have been saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
