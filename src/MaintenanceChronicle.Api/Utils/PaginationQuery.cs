using Microsoft.AspNetCore.Mvc;

namespace MaintenanceChronicle.Api.Utils;

[ModelBinder(BinderType = typeof(PaginationQueryModelBinder))]
public record PaginationQuery(int Page, int PageSize);
