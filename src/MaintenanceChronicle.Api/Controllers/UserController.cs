using System.Globalization;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Roles.Dto;
using MaintenanceChronicle.Application.Contracts.Users.Commands;
using MaintenanceChronicle.Application.Contracts.Users.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Users.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceChronicle.Api.Controllers;

//Makes endpoints accessible only for users with Admin or GlobalAdmin roles
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.GlobalAdmin}")]
[ApiController]
public class UserController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a user with the given information
    /// </summary>
    /// <param name="createNewUserDto">Information that admin provides</param>
    /// <returns>The id of created user</returns>
    [HttpPost("api/v1/users")]
    public async Task<ActionResult<Guid>> CreateUser(
        [FromBody] CreateNewUserDto createNewUserDto
    )
    {
        var createNewUserCommand = new CreateNewUserCommand(createNewUserDto, HttpContext.User.GetUserId(), HttpContext.User.GetTenantId());
        var userId = await mediator.Send(createNewUserCommand);

        var addRolesToUserCommand = new ManageRolesForUserCommand(
            new UserRolesDto
            {
                UserId = userId,
                RoleIds = createNewUserDto.Roles
            },
            HttpContext.User.GetUserId(),
            HttpContext.User.GetTenantId()
        );
        await mediator.Send(addRolesToUserCommand);

        return Ok(userId);
    }

    /// <summary>
    /// Generates and sends a token for the user to reset their password
    /// </summary>
    /// <param name="email">Users email that specifies which user should get the email</param>
    /// <returns></returns>
    [HttpPost("api/v1/users/send-password-create")]
    public async Task<ActionResult> GeneratePasswordCreateEmail([FromQuery] string email)
    {
        var confTokenCommand = new GenerateEmailConfirmTokenCommand(email);
        var confToken = await mediator.Send(confTokenCommand);

        var generateToken = new GeneratePasswordResetTokenCommand(email);
        var token = await mediator.Send(generateToken);

        var generatePasswordResetEmailForUserCommand = new GeneratePasswordCreateEmailForUserCommand(email, token, confToken);
        var emailToBeSent = await mediator.Send(generatePasswordResetEmailForUserCommand);

        var createEmailToBeSendCommand = new CreateNewEmailMessageCommand(emailToBeSent);
        await mediator.Send(createEmailToBeSendCommand);

        return Ok();
    }

    /// <summary>
    /// Updates a user with the given information
    /// </summary>
    /// <param name="id">Edited user id</param>
    /// <param name="userDetailDto">Information that admin provides</param>
    /// <returns></returns>
    [HttpPatch("api/v1/users/{id:guid}")]
    public async Task<ActionResult> UpdateUser(
        [FromRoute]Guid id,
        [FromBody] JsonPatchDocument<UpdateUserDetailDto> userDetailDto
    )
    {
        var createNewUserCommand = new UpdateUserCommand(userDetailDto, id, HttpContext.User.GetUserId());
        await mediator.Send(createNewUserCommand);

        return NoContent();
    }

    /// <summary>
    /// Updates user roles
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="roles">New roles</param>
    /// <returns></returns>
    [HttpPost("api/v1/users/{id:guid}/roles")]
    public async Task<ActionResult> ManageUserRoles(
        [FromRoute] Guid id,
        [FromBody] RoleDetailDto[] roles
    )
    {
        var userDetailDto = new UserRolesDto
        {
            UserId = id,
            RoleIds = roles.Select(r => r.Id).ToArray()
        };
        var manageRoles = new ManageRolesForUserCommand(userDetailDto, HttpContext.User.GetUserId(), User.GetTenantId());
        await mediator.Send(manageRoles);

        return NoContent();
    }

    /// <summary>
    /// Gets a page of users
    /// </summary>
    /// <returns>A page of <see cref="UserListDto"/> with a link to the next page</returns>
    [HttpGet("api/v1/users")]
    public async Task<ActionResult<PagedResponse<UserListDto>>> GetUserList(
        [FromQuery] PaginationQuery pagination)
    {
        var usersQuery = new GetListOfEntityQuery<UserListDto>(new PageRequest(pagination.Page, pagination.PageSize));
        var users = await mediator.Send(usersQuery);

        string? next = null;
        if (users.Count > pagination.PageSize)
        {
            var queryString = QueryString.Create(
            [
                new KeyValuePair<string, string?>("page", (pagination.Page + 1).ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("pageSize", pagination.PageSize.ToString(CultureInfo.InvariantCulture))
            ]);
            next = $"{Request.PathBase}{Request.Path}{queryString}";
        }

        return Ok(new PagedResponse<UserListDto>(users.Take(pagination.PageSize).ToList(), next));
    }

    /// <summary>
    /// Gets a specific user by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>user</returns>
    [HttpGet("api/v1/users/{id:guid}")]
    public async Task<ActionResult> GetUser(
        [FromRoute] Guid id
    )
    {
        var userQuery = new GetEntityByIdQuery<UserDetailDto>(id);
        var user = await mediator.Send(userQuery);

        return Ok(user);
    }

    /// <summary>
    /// Gets the list of locations in which the specified user is contact
    /// </summary>
    /// <param name="id">User Id</param>
    /// <returns>List of locations</returns>
    [HttpGet("api/v1/users/{id:guid}/locations")]
    public async Task<ActionResult<List<LocationInListForContactDto>>> GetUsersLocations([FromRoute] Guid id)
    {
        var query = new GetListOfLocationsForContactUserQuery(id);
        var locations = await mediator.Send(query);

        return Ok(locations);
    }

    /// <summary>
    /// Gets all the available roles for user
    /// </summary>
    /// <returns>List of roles</returns>
    [HttpGet("api/v1/users/roles")]
    public async Task<ActionResult<List<RoleDetailDto>>> GetRoles()
    {
        var query = new GetListOfEntityQuery<RoleDetailDto>();
        var roles = await mediator.Send(query);

        return Ok(roles);
    }
}
