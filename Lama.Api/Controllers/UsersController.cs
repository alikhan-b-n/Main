using Lama.Api.Auth;
using Lama.Application.AccessControl;
using Lama.Domain.AccessControl.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>Admin panel: only an administrator creates and manages CRM accounts.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
[TypeFilter(typeof(AuthExceptionFilter))]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetUsersQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseRole(request.Role, out var role))
            return BadRequest(InvalidRole(request.Role));

        var user = await _mediator.Send(
            new CreateUserCommand(request.Email, request.FullName, request.Password, role), cancellationToken);

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseRole(request.Role, out var role))
            return BadRequest(InvalidRole(request.Role));

        // An administrator must not lock themselves out in one click
        if (id == User.UserId() && (role != UserRole.Admin || !request.IsActive))
            return Conflict(new { message = "You cannot remove your own administrator access", code = "self_demote" });

        await _mediator.Send(new UpdateUserCommand(id, request.FullName, role, request.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetUserPasswordCommand(id, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (id == User.UserId())
            return Conflict(new { message = "You cannot delete your own account", code = "self_delete" });

        await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }

    private static bool TryParseRole(string value, out UserRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    private static object InvalidRole(string value) =>
        new { message = $"Invalid role: {value}. Valid values: {string.Join(", ", Enum.GetNames<UserRole>())}" };
}

public record CreateUserRequest(string Email, string FullName, string Password, string Role);

public record UpdateUserRequest(string FullName, string Role, bool IsActive);

public record ResetPasswordRequest(string NewPassword);
