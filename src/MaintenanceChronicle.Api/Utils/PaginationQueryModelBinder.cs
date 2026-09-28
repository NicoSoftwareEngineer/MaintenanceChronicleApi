using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace MaintenanceChronicle.Api.Utils;

public class PaginationQueryModelBinder(IOptions<PaginationOptions> paginationOptions) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var query = bindingContext.HttpContext.Request.Query;
        var options = paginationOptions.Value;
        var page = 1;
        var pageSize = options.DefaultPageSize;
        var validPage = true;
        var validPageSize = true;

        if (query.TryGetValue("page", out var pageValues) &&
            (pageValues.Count != 1 ||
             !int.TryParse(pageValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out page)))
        {
            validPage = false;
            bindingContext.ModelState.AddModelError("page", "Page must be an integer.");
        }

        if (query.TryGetValue("pageSize", out var pageSizeValues) &&
            (pageSizeValues.Count != 1 ||
             !int.TryParse(pageSizeValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out pageSize)))
        {
            validPageSize = false;
            bindingContext.ModelState.AddModelError("pageSize", "Page size must be an integer.");
        }

        if (validPageSize && (pageSize < 1 || pageSize > options.MaxPageSize))
        {
            validPageSize = false;
            bindingContext.ModelState.AddModelError("pageSize", $"Page size must be between 1 and {options.MaxPageSize}.");
        }

        if (validPage && (page < 1 || page == int.MaxValue ||
                          (validPageSize && ((long)page - 1) * pageSize > int.MaxValue)))
        {
            bindingContext.ModelState.AddModelError("page", "Page must be positive and within the supported range.");
        }

        bindingContext.Result = ModelBindingResult.Success(new PaginationQuery(page, pageSize));
        return Task.CompletedTask;
    }
}
