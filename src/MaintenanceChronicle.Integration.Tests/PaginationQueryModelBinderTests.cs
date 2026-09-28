using MaintenanceChronicle.Api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace MaintenanceChronicle.Integration.Tests;

public class PaginationQueryModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_UsesConfiguredDefault_WhenQueryIsEmpty()
    {
        var context = await BindAsync("");

        Assert.True(context.ModelState.IsValid);
        Assert.Equal(new PaginationQuery(1, 5), context.Result.Model);
    }

    [Fact]
    public async Task BindModelAsync_UsesRequestedPageAndSize()
    {
        var context = await BindAsync("?page=2&pageSize=10");

        Assert.True(context.ModelState.IsValid);
        Assert.Equal(new PaginationQuery(2, 10), context.Result.Model);
    }

    [Theory]
    [InlineData("?page=0", "page")]
    [InlineData("?page=abc", "page")]
    [InlineData("?page=2147483646&pageSize=50", "page")]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=51", "pageSize")]
    [InlineData("?pageSize=abc", "pageSize")]
    public async Task BindModelAsync_RejectsInvalidPaging(string queryString, string propertyName)
    {
        var context = await BindAsync(queryString);

        Assert.False(context.ModelState.IsValid);
        Assert.True(context.ModelState.ContainsKey(propertyName));
    }

    private static async Task<ModelBindingContext> BindAsync(string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(queryString);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(PaginationQuery));
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext, new CompositeValueProvider(), metadata, bindingInfo: null, modelName: "pagination");
        var options = Options.Create(new PaginationOptions { DefaultPageSize = 5, MaxPageSize = 50 });
        var binder = new PaginationQueryModelBinder(options);

        await binder.BindModelAsync(bindingContext);

        return bindingContext;
    }
}
