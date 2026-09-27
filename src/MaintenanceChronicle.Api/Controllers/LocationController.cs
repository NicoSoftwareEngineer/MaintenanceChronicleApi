using MaintenanceChronicle.Application.Contracts.Customers.Queries;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Application.Contracts.Locations.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Api.Utils;
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
public class LocationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates customer with the given information
    /// </summary>
    /// <param name="newLocationDto">Information that admin provides</param>
    /// <returns>New customer id</returns>
    [HttpPost("api/v1/locations")]
    public async Task<ActionResult<Guid>> CreateLocation(
        [FromBody] NewLocationDto newLocationDto
    )
    {
        var createNewLocationCommand = new CreateNewLocationCommand(
            newLocationDto,
            User.GetUserId(),
            User.GetTenantId()
        );
        var locationId = await mediator.Send(createNewLocationCommand);

        return Ok(locationId);
    }

    /// <summary>
    /// Updates customer with the given information
    /// </summary>
    /// <param name="id"></param>
    /// <param name="patch">Information that admin provides</param>
    /// <returns>Updated <see cref="ManageLocationDetailDto"/></returns>
    [HttpPatch("api/v1/locations/{id:guid}")]
    public async Task<ActionResult<ManageLocationDetailDto>> UpdateLocation(
        [FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ManageLocationDetailDto> patch
    )
    {
        var updateLocationCommand = new UpdateLocationCommand(
            patch,
            id,
            User.GetUserId()
        );
        var updatedLocation = await mediator.Send(updateLocationCommand);

        return Ok(updatedLocation);
    }

    /// <summary>
    /// Gets the list of locations
    /// </summary>
    /// <returns>List of <see cref="LocationInListDto"/></returns>
    [HttpGet("/api/v1/locations")]
    public async Task<ActionResult<List<LocationInListDto>>> GetLocationList()
    {
        var getLocationsQuery = new GetListOfEntityQuery<LocationInListDto>();
        var locationList = await mediator.Send(getLocationsQuery);

        return Ok(locationList);
    }

    /// <summary>
    /// Get location by id
    /// </summary>
    /// <param name="id">Id that user provides</param>
    /// <returns><see cref="LocationDetailDto"/></returns>
    [HttpGet("/api/v1/locations/{id:guid}")]
    public async Task<ActionResult<List<LocationDetailDto>>> GetLocation(Guid id)
    {
        var getLocationQuery = new GetEntityByIdQuery<LocationDetailDto>(id);
        var location = await mediator.Send(getLocationQuery);

        return Ok(location);
    }

    /// <summary>
    /// Delete the specified location
    /// </summary>
    /// <param name="id">ID of location to delete</param>
    /// <returns></returns>
    [HttpDelete("/api/v1/locations/{id:guid}")]
    public async Task<ActionResult> DeleteLocation(Guid id)
    {
        var deleteCommand = new DeleteEntityByIdCommand<Location>(id, User.GetUserId());
        await mediator.Send(deleteCommand);

        return NoContent();
    }

    /// <summary>
    /// Adds contact to Location
    /// </summary>
    /// <param name="id">Id of location to add contact to</param>
    /// <param name="contactInListDto">Array of <see cref="LocationContactInListDto"/> containing contacts of location</param>
    /// <returns></returns>
    [HttpPost("/api/v1/locations/{id:guid}/contacts")]
    public async Task<ActionResult> ManageContactsToLocation([FromRoute] Guid id,[FromBody]LocationContactInListDto[] contactInListDto)
    {
        var assignCommand =
            new ManageContactsInLocationCommand(id, contactInListDto, User.GetUserId(), User.GetTenantId());
        await mediator.Send(assignCommand);

        return NoContent();
    }

    /// <summary>
    /// Gets contacts for specified location
    /// </summary>
    /// <param name="id">ID of location to get contacts for</param>
    /// <returns>List of <see cref="LocationContactInListDto"/></returns>
    [AllowAnonymous]
    [AllowTenantlessDataAccess]
    [HttpGet("/api/v1/locations/{id:guid}/contacts")]
    public async Task<ActionResult<List<LocationContactInListDto>>> GetContactsForLocation([FromRoute] Guid id)
    {
        var getContactsQuery = new GetListOfContactsForLocationQuery(id);
        var contacts = await mediator.Send(getContactsQuery);

        return Ok(contacts);
    }

    /// <summary>
    /// Gets possible contacts for location
    /// </summary>
    /// <returns>List of <see cref="LocationContactInListDto"/>, that can be added as contact for location</returns>
    [HttpGet("/api/v1/locations/contacts")]
    public async Task<ActionResult<List<LocationContactInListDto>>> GetPossibleContactsForLocation()
    {
        var getContactsQuery = new GetListOfEntityQuery<LocationContactInListDto>();
        var contacts = await mediator.Send(getContactsQuery);

        return Ok(contacts);
    }

    /// <summary>
    /// Gets list of machines for the specified location
    /// </summary>
    /// <param name="id">User specified location id</param>
    /// <returns>List of <see cref="MachineInListDto"/></returns>
    [HttpGet("/api/v1/locations/{id:guid}/machines")]
    public async Task<ActionResult<List<MachineInListForLocationDto>>> GetMachinesForLocation([FromRoute] Guid id)
    {
        var query = new GetMachinesForLocationQuery(id);
        var machines = await mediator.Send(query);

        return Ok(machines);
    }

    /// <summary>
    /// Gets customer for the specified location
    /// </summary>
    /// <param name="id">ID of location for which to get the customer</param>
    /// <returns><see cref="CustomerDetailForLocationDto"/> from location</returns>
    [HttpGet("/api/v1/locations/{id:guid}/customer")]
    public async Task<ActionResult<CustomerDetailForLocationDto>> GetCustomerForLocation([FromRoute] Guid id)
    {
        var query = new GetCustomerDetailForLocationQuery(id);
        var customer = await mediator.Send(query);

        return Ok(customer);
    }
}
