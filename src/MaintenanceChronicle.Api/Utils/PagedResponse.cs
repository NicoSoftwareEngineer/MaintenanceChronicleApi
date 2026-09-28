namespace MaintenanceChronicle.Api.Utils;

public record PagedResponse<TEntity>(IReadOnlyList<TEntity> Items, string? Next);
