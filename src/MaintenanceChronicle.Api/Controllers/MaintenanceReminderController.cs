using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceChronicle.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.GlobalAdmin},{RoleTypes.Technician}")]
[Route("api/v1/maintenance-reminders")]
public class MaintenanceReminderController(IMediator mediator) : Controller
{
    /// <summary>
    /// Creates a new maintenance reminder
    /// </summary>
    /// <param name="reminderDto">
    /// The new maintenance reminder data
    /// </param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> CreateMaintenanceReminder([FromBody] NewMaintenanceReminderDto reminderDto)
    {
        var command = new CreateNewMaintenanceReminderCommand(reminderDto, User.GetUserId(), User.GetTenantId());
        await mediator.Send(command);
        
        return Ok();
    }

    /// <summary>
    /// Updates a maintenance reminder
    /// </summary>
    /// <param name="id">Id of updated maintenance reminder</param>
    /// <param name="reminderDto"><see cref="JsonPatchDocument{MaintenanceReminderDetailDto}"/> with instructions on what to change</param>
    /// <returns>The updated <see cref="MaintenanceReminderDetailDto"/></returns>
    [HttpPatch("{id}")]
    public async Task<ActionResult<MaintenanceReminderDetailDto>> UpdateMaintenanceReminder([FromRoute] Guid id, [FromBody] JsonPatchDocument<MaintenanceReminderDetailDto>reminderDto)
    {
        var command = new UpdateMaintenanceReminderCommand(id, reminderDto, User.GetUserId());
        var result = await mediator.Send(command);

        return Ok(result);
    }
}
