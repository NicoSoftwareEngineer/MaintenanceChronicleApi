namespace MaintenanceChronicle.Api.Utils;

public class PaginationOptions
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; set; }
    public int MaxPageSize { get; set; }
}
