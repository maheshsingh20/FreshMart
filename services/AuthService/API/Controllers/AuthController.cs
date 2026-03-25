using AuthService.Application.Commands;
using AuthService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(Register), result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand cmd, CancellationToken ct)
    {
        await mediator.Send(cmd, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserProfileQuery(CurrentUserId), ct);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GoogleAuthCommand(req.IdToken), ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpCommand cmd, CancellationToken ct)
    {
        await mediator.Send(cmd, ct);
        return Ok(new { message = "If the email exists, an OTP has been sent." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
        => await SendOtp(new SendOtpCommand(req.Email, "password-reset"), ct);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { message = "Password reset successfully." });
    }

    // ── Address endpoints ────────────────────────────────────────────────────

    [HttpGet("addresses")]
    [Authorize]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAddressesQuery(CurrentUserId), ct);
        return Ok(result);
    }

    [HttpPost("addresses")]
    [Authorize]
    public async Task<IActionResult> SaveAddress([FromBody] AddressRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new SaveAddressCommand(
            CurrentUserId, req.Label, req.Line1, req.Line2,
            req.City, req.State, req.Pincode, req.Country, req.IsDefault), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPut("addresses/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] AddressRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new SaveAddressCommand(
            CurrentUserId, req.Label, req.Line1, req.Line2,
            req.City, req.State, req.Pincode, req.Country, req.IsDefault, id), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("addresses/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteAddressCommand(CurrentUserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return NoContent();
    }

    [HttpPatch("addresses/{id:guid}/default")]
    [Authorize]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SetDefaultAddressCommand(CurrentUserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return NoContent();
    }
}

public record GoogleAuthRequest(string IdToken);
public record ForgotPasswordRequest(string Email);
public record AddressRequest(
    string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault);
