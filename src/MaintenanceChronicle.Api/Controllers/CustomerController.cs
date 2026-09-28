using System.Globalization;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application.Contracts.Customers.Commands;
using MaintenanceChronicle.Application.Contracts.Customers.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceChronicle.Api.Controllers;

//Makes endpoints accessible only for users with Admin or GlobalAdmin roles
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.GlobalAdmin},{RoleTypes.Technician}")]
[ApiController]
public class CustomerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates customer with the given information
    /// </summary>
    /// <param name="newCustomerDto">Information that admin provides</param>
    /// <returns>New customer id</returns>
    [HttpPost("api/v1/customers")]
    public async Task<ActionResult<Guid>> CreateCustomer(
        [FromBody] NewCustomerDto newCustomerDto
    )
    {
        var createNewCustomerDto = new CreateNewCustomerCommand(
            newCustomerDto,
            User.GetUserId(),
            User.GetTenantId()
        );
        var customerId = await mediator.Send(createNewCustomerDto);

        return Ok(customerId);
    }

    /// <summary>
    /// Updates customer with the given information
    /// </summary>
    /// <param name="id">ID of customer to be updated</param>
    /// <param name="patch">Information that admin provides</param>
    /// <returns>Updated <see cref="ManageCustomerDetailDto"/></returns>
    [HttpPatch("api/v1/customers/{id:guid}")]
    public async Task<ActionResult<ManageCustomerDetailDto>> UpdateCustomer(
        [FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ManageCustomerDetailDto> patch
    )
    {
        var updateCustomerCommand = new UpdateCustomerCommand(
            patch,
            id,
            User.GetUserId()
        );
        var updatedCustomer = await mediator.Send(updateCustomerCommand);

        return Ok(updatedCustomer);
    }

    /// <summary>
    /// Gets a page of customers
    /// </summary>
    /// <returns>A page of <see cref="CustomerInListDto"/> with a link to the next page</returns>
    [HttpGet("api/v1/customers")]
    public async Task<ActionResult<PagedResponse<CustomerInListDto>>> GetCustomerList(
        [FromQuery] PaginationQuery pagination)
    {
        var getCustomerListQuery = new GetListOfEntityQuery<CustomerInListDto>(new PageRequest(pagination.Page, pagination.PageSize));
        var customers = await mediator.Send(getCustomerListQuery);

        string? next = null;
        if (customers.Count > pagination.PageSize)
        {
            var queryString = QueryString.Create(
            [
                new KeyValuePair<string, string?>("page", (pagination.Page + 1).ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("pageSize", pagination.PageSize.ToString(CultureInfo.InvariantCulture))
            ]);
            next = $"{Request.PathBase}{Request.Path}{queryString}";
        }

        return Ok(new PagedResponse<CustomerInListDto>(customers.Take(pagination.PageSize).ToList(), next));
    }

    /// <summary>
    /// Gets a specific customer by id
    /// </summary>
    /// <param name="id">Id of customer to get</param>
    /// <returns>The requested <see cref="CustomerDetailDto"/>></returns>
    [HttpGet("api/v1/customers/{id:guid}")]
    public async Task<ActionResult> GetCustomer(
        [FromRoute] Guid id
    )
    {
        var customerQuery = new GetEntityByIdQuery<CustomerDetailDto>(id);
        var customer = await mediator.Send(customerQuery);

        return Ok(customer);
    }

    /// <summary>
    /// Deletes a specific customer by id
    /// </summary>
    /// <param name="id">Id of customer to delete</param>
    /// <returns></returns>
    [HttpDelete("api/v1/customers/{id:guid}")]
    public async Task<ActionResult> DeleteCustomer(
        [FromRoute] Guid id
    )
    {
        var deleteCommand = new DeleteEntityByIdCommand<Customer>(id, User.GetUserId());
        await mediator.Send(deleteCommand);

        return NoContent();
    }

    /// <summary>
    /// Gets list of locations for the specified customer
    /// </summary>
    /// <param name="id">[Guid] customer id</param>
    /// <returns>List of <see cref="LocationInListDto"/>></returns>
    [HttpGet("api/v1/customers/{id:guid}/locations")]
    public async Task<ActionResult<List<LocationInListDto>>> GetLocationsForCustomer([FromRoute] Guid id)
    {
        var locationsQuery = new GetLocationsForCustomerQuery(id);
        var locations = await mediator.Send(locationsQuery);

        return Ok(locations);
    }
}
