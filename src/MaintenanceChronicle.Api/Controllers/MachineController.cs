using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Machines.Commands;
using MaintenanceChronicle.Application.Contracts.Machines.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Machines.Commands;
using MaintenanceChronicle.Api.Utils;
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
public class MachineController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates new machine
    /// </summary>
    /// <param name="newMachineDetail">Information from user</param>
    /// <returns>New machine id</returns>
    [HttpPost("/api/v1/machines")]
    public async Task<ActionResult<Guid>> CreateMachine([FromBody] NewMachineDto newMachineDetail)
    {
        var createCommand = new CreateNewMachineCommand(newMachineDetail, User.GetUserId(), User.GetTenantId());
        var machineId = await mediator.Send(createCommand);

        return Ok(machineId);
    }

    /// <summary>
    /// Updates machine info
    /// </summary>
    /// <param name="id">Updated machine id</param>
    /// <param name="patch"><see cref="JsonPatchDocument{MManageMachineDetailDto}"/> With instruction on what to replace</param>
    /// <returns>Updated <see cref="ManageMachineDetailDto"/></returns>
    [HttpPatch("/api/v1/machines/{id:guid}")]
    public async Task<ActionResult<ManageMachineDetailDto>> UpdateMachine([FromRoute] Guid id, [FromBody] JsonPatchDocument<ManageMachineDetailDto> patch)
    {
        var command = new UpdateMachineCommand(patch, id, User.GetUserId());
        var result = await mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Soft deletes machine by specified id
    /// </summary>
    /// <param name="id">ID of machine to delete</param>
    /// <returns></returns>
    [HttpDelete("/api/v1/machines/{id:guid}")]
    public async Task<ActionResult> DeleteMachine([FromRoute] Guid id)
    {
        var command = new DeleteEntityByIdCommand<Machine>(id, User.GetUserId());
        await mediator.Send(command);

        return Ok();
    }

    /// <summary>
    /// Gets machine by specified id
    /// </summary>
    /// <param name="id">ID of machine to query for</param>
    /// <returns><see cref="MachineDetailDto"/></returns>
    [AllowAnonymous]
    [AllowTenantlessDataAccess]
    [HttpGet("/api/v1/machines/{id:guid}")]
    public async Task<ActionResult<MachineDetailDto>> GetMachineById([FromRoute] Guid id)
    {
        var machineQuery = new GetEntityByIdQuery<MachineDetailDto>(id);
        var machine = await mediator.Send(machineQuery);

        return Ok(machine);
    }

    /// <summary>
    /// Gets list of machines
    /// </summary>
    /// <returns>List of <see cref="MachineInListDto"/></returns>
    [HttpGet("/api/v1/machines")]
    public async Task<ActionResult<List<MachineInListDto>>> GetMachineList()
    {
        var query = new GetListOfEntityQuery<MachineInListDto>();
        var machines = await mediator.Send(query);

        return Ok(machines);
    }

    /// <summary>
    /// Gets MaintenanceRecords for the specified machine
    /// </summary>
    /// <param name="id">ID of machine for which to get records</param>
    /// <returns>List of <see cref="MaintenanceRecordInListForMachineDto"/></returns>
    [HttpGet("/api/v1/machines/{id:guid}/maintenance-records")]
    public async Task<ActionResult<List<MaintenanceRecordInListForMachineDto>>> GetMaintenanceRecordsForMachine(
        [FromRoute] Guid id)
    {
        var query = new GetMaintenanceRecordsByMachineIdQuery(id);
        var records = await mediator.Send(query);

        return Ok(records);
    }

    /// <summary>
    /// Gets Location for the specified machine
    /// </summary>
    /// <param name="id">Specified machine id</param>
    /// <returns>Machines <see cref="LocationInListDto"/></returns>
    [AllowAnonymous]
    [AllowTenantlessDataAccess]
    [HttpGet("/api/v1/machines/{id:guid}/location")]
    public async Task<ActionResult<List<LocationInListDto>>> GetLocationForMachine(
        [FromRoute] Guid id)
    {
        var query = new GetLocationForMachineQuery(id);
        var location = await mediator.Send(query);

        return Ok(location);
    }

    /// <summary>
    /// Gets all the reminders for one machine
    /// </summary>
    /// <param name="id">Machine id</param>
    /// <returns>List of <see cref="MaintenanceReminderInListForMachineDto"/></returns>
    [HttpGet("/api/v1/machines/{id:guid}/maintenance-reminders")]
    public async Task<ActionResult<List<MaintenanceReminderInListForMachineDto>>> GetMaintenanceRemindersForMachine([FromRoute] Guid id)
    {
        var query = new GetAllMaintenanceRemindersForMachineQuery(id);
        var reminders = await mediator.Send(query);
        return Ok(reminders);
    }
}
