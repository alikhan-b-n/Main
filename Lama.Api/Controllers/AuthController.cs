using Lama.Api.Auth;
using Lama.Application.AccessControl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/auth")]
[TypeFilter(typeof(AuthExceptionFilter))]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, JwtTokenService tokens, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _tokens = tokens;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new AuthenticateUserCommand(request.Email, request.Password), cancellationToken);
        if (user == null)
        {
            // Never say which part was wrong, and never log the password
            _logger.LogWarning("Failed sign-in attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var (token, expiresAt) = _tokens.Issue(user);
        return Ok(new LoginResponse(token, expiresAt, user));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var id = User.UserId();
        if (id == null)
            return Unauthorized();

        var user = await _mediator.Send(new GetUserByIdQuery(id.Value), cancellationToken);
        // The account could have been deleted or switched off while the token was still valid
        return user is { IsActive: true } ? Ok(user) : Unauthorized();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var id = User.UserId();
        if (id == null)
            return Unauthorized();

        var changed = await _mediator.Send(
            new ChangeOwnPasswordCommand(id.Value, request.CurrentPassword, request.NewPassword), cancellationToken);

        return changed ? NoContent() : BadRequest(new { message = "Current password is wrong", code = "wrong_password" });
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
