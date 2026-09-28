using System.Security.Claims;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.RefreshTokens.Commands;
using MaintenanceChronicle.Application.Contracts.RefreshTokens.Queries;
using MaintenanceChronicle.Application.Contracts.Roles.Dto;
using MaintenanceChronicle.Application.Contracts.Tenants.Commands;
using MaintenanceChronicle.Application.Contracts.Tenants.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Users.Commands;
using MaintenanceChronicle.Application.Contracts.Users.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Users.Queries;
using MaintenanceChronicle.Application.Contracts.Users.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.UserTenant.Commands;
using MaintenanceChronicle.Application.Contracts.UserTenant.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Utilities.Constants;
using MaintenanceChronicle.Utilities.Error;
using MaintenanceChronicle.Utilities.Helpers;
using MaintenanceChronicle.Utilities.Options;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MaintenanceChronicle.Api.Controllers;

[ApiController]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Logs in the user with the given information
    /// </summary>
    /// <param name="loginDto">Email and password to login user</param>
    /// <param name="jwtOptions">JWT options registered in service collection</param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/login")]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto, [FromServices] IOptions<JwtOptions> jwtOptions)
    {
        var canLogInCommand = new CheckUserLogInCommand(loginDto);
        var canLogIn = await mediator.Send(canLogInCommand);
        if (!canLogIn.Succeeded)
        {
            throw new BadRequestException(ErrorType.InvalidLogIn);
        }

        var claimsListForUserCommand = new GenerateClaimsListForUserCommand(loginDto.Email);
        var claims = await mediator.Send(claimsListForUserCommand);

        var getTenantIdForUserCommand = new GetTenantIdFromUserQuery(loginDto.Email);
        var tenantId = await mediator.Send(getTenantIdForUserCommand);

        var userTenantClaimDto = new UserTenantClaimDto
        {
            Email = loginDto.Email,
            TenantId = tenantId
        };

        var claimsWithTenantIdCommand = new AddTenantClaimsListCommand(userTenantClaimDto, claims);
        var claimsWithTenantId = await mediator.Send(claimsWithTenantIdCommand);

        var generateAccessToken = new GenerateAccessTokenFromClaimsCommand(claimsWithTenantId);
        var accessToken = await mediator.Send(generateAccessToken);

        var generateRefreshToken = new GenerateRefreshTokenForUserCommand(loginDto.Email, Request.Headers.UserAgent.ToString());
        var refreshToken = await mediator.Send(generateRefreshToken);

        var activeTokenName = $"Auth-{loginDto.Email.Hash()}";

        Response.Cookies.Append(activeTokenName.UriEscape(), refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // For HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays)
        });

        Response.Cookies.Append(TokenConstants.ActiveTokenName, activeTokenName, new CookieOptions
        {
            HttpOnly = false,
            Secure = false, // For HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays)
        });

        return Ok(new { Token = accessToken, Name = activeTokenName.UriEscape() });
    }

    /// <summary>
    /// Refreshes users access token, using the RefreshToken stored in cookies.
    /// </summary>
    /// <param name="jwtOptions">JWTOptions from service collection</param>
    /// <returns>New token model</returns>
    /// <exception cref="UnauthorizedRequestException">Token is invalid, null or expired</exception>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/refresh-token")]
    public async Task<ActionResult> RefreshToken([FromServices] IOptions<JwtOptions> jwtOptions)
    {
        if (!Request.Cookies.TryGetValue(TokenConstants.ActiveTokenName, out var activeTokenName))
        {
            throw new UnauthorizedRequestException(ErrorType.TokenNotFound);
        }
        if (!Request.Cookies.TryGetValue(activeTokenName.UriEscape(), out var incomingRefreshToken))
        {
            throw new UnauthorizedRequestException(ErrorType.TokenNotFound);
        }

        var getValidTokenQuery = new GetStoredRefreshTokenQuery(incomingRefreshToken);
        var validStoredToken = await mediator.Send(getValidTokenQuery);
        if (validStoredToken == null)
        {
            throw new UnauthorizedRequestException(ErrorType.InvalidRefreshToken);
        }

        var userQuery = new GetEntityByIdQuery<UserDetailDto>(validStoredToken.UserId);
        var user = await mediator.Send(userQuery);

        var claimsListForUserCommand = new GenerateClaimsListForUserCommand(user.Email);
        var claims = await mediator.Send(claimsListForUserCommand);

        var getTenantIdForUserCommand = new GetTenantIdFromUserQuery(user.Email);
        var tenantId = await mediator.Send(getTenantIdForUserCommand);

        var claimsWithTenantIdCommand = new AddTenantClaimsListCommand(new UserTenantClaimDto{ Email = user.Email, TenantId = tenantId }, claims);
        var claimsWithTenantId = await mediator.Send(claimsWithTenantIdCommand);

        var generateAccessToken = new GenerateAccessTokenFromClaimsCommand(claimsWithTenantId);
        var accessToken = await mediator.Send(generateAccessToken);

        var generateRefreshToken = new GenerateRefreshTokenForUserCommand(user.Email, Request.Headers.UserAgent.ToString());
        var refreshToken = await mediator.Send(generateRefreshToken);

        var revokeExistingTokenCommand = new RevokeRefreshTokenCommand(incomingRefreshToken);
        await mediator.Send(revokeExistingTokenCommand);

        Response.Cookies.Append(activeTokenName.UriEscape(), refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // For HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays)
        });

        return Ok(new { Token = accessToken, Name = activeTokenName.UriEscape() });
    }

    /// <summary>
    /// Logs out the user and revokes the refresh token
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UnauthorizedRequestException"></exception>
    [Authorize]
    [HttpGet("api/v1/auth/logout")]
    public async Task<ActionResult> Logout()
    {
        if (!Request.Cookies.TryGetValue(TokenConstants.ActiveTokenName, out var activeTokenName))
        {
            throw new UnauthorizedRequestException(ErrorType.TokenNotFound);
        }
        if (!Request.Cookies.TryGetValue(activeTokenName.UriEscape(), out var incomingRefreshToken))
        {
            throw new UnauthorizedRequestException(ErrorType.TokenNotFound);
        }

        var revokeExistingTokenCommand = new RevokeRefreshTokenCommand(incomingRefreshToken);
        await mediator.Send(revokeExistingTokenCommand);

        await HttpContext.SignOutAsync();
        Response.Cookies.Delete(activeTokenName.UriEscape());
        return NoContent();
    }

    /// <summary>
    /// Registers a new user and tenant with the given information. The user is not logged in after registration
    /// </summary>
    /// <param name="registerUserTenantDto">Information needed to create new user with specified password and tenant</param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/register-user-tenant")]
    public async Task<ActionResult<Guid>> RegisterUserTenant(
        [FromBody] RegisterUserTenantDto registerUserTenantDto
    )
    {
        var registerNewUserCommand = new RegisterUserAndTenantCommand(registerUserTenantDto);
        var result = await mediator.Send(registerNewUserCommand);

        var getRoleByNameCommand = new GetEntityByNameQuery<RoleDetailDto>(RoleTypes.Admin);
        var adminRole = await mediator.Send(getRoleByNameCommand);

        var addRolesToUserCommand = new ManageRolesForUserCommand(
            new UserRolesDto
            {
                UserId = result.UserId,
                RoleIds = new Guid[1] { adminRole.Id }
            },
            result.UserId.ToString(),
            result.TenantId.ToString()
        );
        await mediator.Send(addRolesToUserCommand);

        return NoContent();
    }

    /// <summary>
    /// Generates and sends a token for the user to confirm their email
    /// </summary>
    /// <param name="email">Users email that specifies which user should get the email</param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/send-email-confirm-email")]
    public async Task<ActionResult> GenerateEmailConfirmationEmail([FromQuery] string email)
    {
        var confTokenCommand = new GenerateEmailConfirmTokenCommand(email);
        var confToken = await mediator.Send(confTokenCommand);

        var generateEmailConfirmationTokenForUserCommand = new GenerateEmailConfirmationEmailForUserCommand(email, confToken);
        var emailToBeSent = await mediator.Send(generateEmailConfirmationTokenForUserCommand);

        var createEmailToBeSendCommand = new CreateNewEmailMessageCommand(emailToBeSent);
        await mediator.Send(createEmailToBeSendCommand);

        return Ok();
    }

    /// <summary>
    /// Validates the token that the user has received in their email
    /// </summary>
    /// <param name="confirmTokenForUserDto">Email and the given token for email confirmation</param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/validate-token")]
    public async Task<ActionResult> ValidateToken(
        [FromQuery] EmailConfirmTokenForUserDto confirmTokenForUserDto
    )
    {
        var validateEmailConfirmationTokenForUserCommand = new ValidateEmailConfirmationTokenCommand(confirmTokenForUserDto);
        await mediator.Send(validateEmailConfirmationTokenForUserCommand);

        return NoContent();
    }

    /// <summary>
    /// Generates and sends a token for the user to reset their password
    /// </summary>
    /// <param name="email">Users email that specifies which user should get the email</param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/send-password-reset")]
    public async Task<ActionResult> GeneratePasswordResetEmail([FromQuery] string email)
    {
        var generateToken = new GeneratePasswordResetTokenCommand(email);
        var token = await mediator.Send(generateToken);

        var generatePasswordResetEmailForUserCommand = new GeneratePasswordResetEmailForUserCommand(email, token);
        var emailToBeSent = await mediator.Send(generatePasswordResetEmailForUserCommand);

        var createEmailToBeSendCommand = new CreateNewEmailMessageCommand(emailToBeSent);
        await mediator.Send(createEmailToBeSendCommand);

        return Ok();
    }

    /// <summary>
    /// Resets the password for user, when the reset token is valid for that user
    /// </summary>
    /// <param name="userResetPasswordDto"></param>
    /// <returns></returns>
    [AllowTenantlessDataAccess]
    [HttpPost("api/v1/auth/reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] UserResetPasswordDto userResetPasswordDto)
    {
        var command = new ResetPasswordForUserCommand(userResetPasswordDto);
        await mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Get the signed-in user info
    /// </summary>
    /// <returns>LoggedIn user info</returns>
    [HttpGet("api/v1/auth/current-user-info")]
    public async Task<ActionResult<LoggedInUserInfoDto>> GetCurrentUserInfo()
    {
        var query = new GetCurrentUserInfoQuery(User.GetUserId());
        var info = await mediator.Send(query);

        return info;
    }

    /// <summary>
    /// Get current user roles
    /// </summary>
    /// <returns>Current user roles</returns>
    [HttpGet("api/v1/auth/current-user-roles")]
    public ActionResult<string[]> GetCurrentUserRoles()
    {
        var roles = HttpContext.User.GetUserRoles();
        return Ok(roles);
    }
}
