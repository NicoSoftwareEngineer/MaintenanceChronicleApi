using System.Globalization;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.RecordTypes.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceChronicle.Api.Controllers;

//Makes endpoints accessible only for users with Admin or GlobalAdmin roles
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.GlobalAdmin},{RoleTypes.Technician}")]
[ApiController]
public class MaintenanceRecordController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates new MaintenanceRecord, that is assigned to defined machine
    /// </summary>
    /// <param name="recordDto">MaintenanceRecord with user defined info</param>
    /// <returns>New MaintenanceRecord id</returns>
    [HttpPost("/api/v1/maintenance-records/")]
    public async Task<ActionResult<Guid>> CreateMaintenanceRecord([FromBody] NewMaintenanceRecordDto recordDto)
    {
        var command = new CreateNewMaintenanceRecordCommand(recordDto, User.GetUserId(), User.GetTenantId());
        var recordId = await mediator.Send(command);

        return Ok(recordId);
    }

    /// <summary>
    /// Patch is applied to MaintenanceRecord with id from route
    /// </summary>
    /// <param name="id">MaintenanceRecord id</param>
    /// <param name="patch"><see cref="JsonPatchDocument{ManageMaintenanceRecordDetailDto}"/> with instructions on what to change</param>
    /// <returns>Updated <see cref="MachineInMaintenanceRecordDetailDto"/></returns>
    [HttpPatch("/api/v1/maintenance-records/{id:guid}")]
    public async Task<ActionResult<ManageMaintenanceRecordDetailDto>> UpdateMaintenanceRecord([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ManageMaintenanceRecordDetailDto> patch)
    {
        var command = new UpdateMaintenanceRecordCommand(patch, id, User.GetUserId());
        var result = await mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Soft deletes a MaintenanceRecord 
    /// </summary>
    /// <param name="id">MaintenanceRecord id</param>
    /// <returns></returns>
    [HttpDelete("/api/v1/maintenance-records/{id:guid}")]
    public async Task<ActionResult> DeleteMaintenanceRecord([FromRoute] Guid id)
    {
        var command = new DeleteEntityByIdCommand<MaintenanceRecord>(id, User.GetUserId());
        await mediator.Send(command);

        return Ok();
    }

    /// <summary>
    /// Gets MaintenanceRecord with specified id
    /// </summary>
    /// <param name="id">MaintenanceRecord id</param>
    /// <returns><see cref="MaintenanceRecordDetailDto"/> of requested record</returns>
    [HttpGet("/api/v1/maintenance-records/{id:guid}")]
    public async Task<ActionResult<MaintenanceRecordDetailDto>> GetMaintenanceRecordById([FromRoute] Guid id)
    {
        var query = new GetEntityByIdQuery<MaintenanceRecordDetailDto>(id);
        var result = await mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Gets a page of maintenance records
    /// </summary>
    /// <returns>A page of <see cref="MaintenanceRecordInListDto"/> with a link to the next page</returns>
    [HttpGet("/api/v1/maintenance-records/")]
    public async Task<ActionResult<PagedResponse<MaintenanceRecordInListDto>>> GetListOfMaintenanceRecords(
        [FromQuery] PaginationQuery pagination)
    {
        var query = new GetListOfEntityQuery<MaintenanceRecordInListDto>(new PageRequest(pagination.Page, pagination.PageSize));
        var result = await mediator.Send(query);

        string? next = null;
        if (result.Count > pagination.PageSize)
        {
            var queryString = QueryString.Create(
            [
                new KeyValuePair<string, string?>("page", (pagination.Page + 1).ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("pageSize", pagination.PageSize.ToString(CultureInfo.InvariantCulture))
            ]);
            next = $"{Request.PathBase}{Request.Path}{queryString}";
        }

        return Ok(new PagedResponse<MaintenanceRecordInListDto>(result.Take(pagination.PageSize).ToList(), next));
    }

    /// <summary>
    /// Gets list of all maintenance record types
    /// </summary>
    /// <returns>List of <see cref="RecordTypeDto"/></returns>
    [HttpGet("/api/v1/maintenance-records/types/")]
    public async Task<ActionResult<RecordTypeDto>> GetListOfMaintenanceRecordTypes()
    {
        var query = new GetListOfEntityQuery<RecordTypeDto>();
        var result = await mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Gets machine for the specified maintenance record
    /// </summary>
    /// <param name="id">ID of record for which we get the machine for</param>
    /// <returns>Records <see cref="MachineInMaintenanceRecordDetailDto"/></returns>
    [HttpGet("/api/v1/maintenance-records/{id:guid}/machine")]
    public async Task<ActionResult<MachineInMaintenanceRecordDetailDto>> GetMachineForMaintenanceRecord(Guid id)
    {
        var query = new GetMachineByMaintenanceRecordIdQuery(id);
        var machine = await mediator.Send(query);

        return Ok(machine);
    }
}
