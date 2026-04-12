using AuthService.Application.Commands;
using AuthService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthService.API.Controllers;

/// <summary>
/// HTTP API controller for authentication and user identity management.
/// Handles the full authentication lifecycle: registration, email/password login,
/// Google OAuth login, JWT refresh, logout, email verification via OTP,
/// password reset, and user profile management. Also exposes address CRUD
/// endpoints so customers can manage their saved delivery addresses.
/// All write operations are dispatched via MediatR to keep the controller thin.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Resolves the authenticated user's ID from the JWT <c>sub</c> claim,
    /// falling back to <see cref="ClaimTypes.NameIdentifier"/> for compatibility.
    /// Used by endpoints that operate on the currently logged-in user's data.
    /// </summary>
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Registers a new customer account. Validates the command (email format,
    /// password strength, etc.) via FluentValidation before persisting.
    /// Returns 201 Created with the new user's profile on success,
    /// or 400 Bad Request with validation errors.
    /// </summary>
    /// <param name="cmd">Registration details: email, password, first/last name.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// On success, returns a short-lived JWT access token and a long-lived
    /// refresh token. The refresh token is stored hashed in the database.
    /// Returns 401 Unauthorized if credentials are invalid or the account is inactive.
    /// </summary>
    /// <param name="cmd">The email and password credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Issues a new access token using a valid, non-expired refresh token.
    /// The old refresh token is invalidated and a new one is issued (token rotation),
    /// limiting the window of exposure if a refresh token is compromised.
    /// Returns 401 if the refresh token is invalid, expired, or already revoked.
    /// </summary>
    /// <param name="cmd">The refresh token to exchange.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Revokes the provided refresh token, effectively logging the user out.
    /// After this call, the token cannot be used to obtain new access tokens.
    /// The current access token remains valid until it expires (JWTs are stateless),
    /// but the short expiry window limits the risk.
    /// </summary>
    /// <param name="cmd">The refresh token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand cmd, CancellationToken ct)
    {
        await mediator.Send(cmd, ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the authenticated user's profile information.
    /// Used by the frontend on app load to populate the user context
    /// (name, role, email verification status, etc.).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserProfileQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates the authenticated user's profile (first name, last name, phone number).
    /// The <c>UserId</c> is injected from the JWT rather than the request body
    /// to prevent users from updating other accounts.
    /// </summary>
    /// <param name="cmd">The new profile values.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd with { UserId = CurrentUserId }, ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Authenticates or registers a user via Google Sign-In.
    /// Verifies the Google ID token with Google's tokeninfo endpoint, then either
    /// finds the existing account (by Google ID or email) or creates a new one.
    /// Existing email/password accounts are automatically linked to Google on first
    /// Google sign-in. Returns the same JWT response as the password login endpoint.
    /// </summary>
    /// <param name="req">The Google ID token from the frontend Google Sign-In SDK.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GoogleAuthCommand(req.IdToken), ct);
        if (!result.IsSuccess) return Unauthorized(new { result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Sends a one-time password (OTP) to the user's email address.
    /// Used for both email verification (after registration) and password reset.
    /// The response is intentionally vague ("If the email exists, an OTP has been sent")
    /// to prevent email enumeration attacks.
    /// </summary>
    /// <param name="cmd">The email address and the OTP purpose ("email-verification" or "password-reset").</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpCommand cmd, CancellationToken ct)
    {
        await mediator.Send(cmd, ct);
        return Ok(new { message = "If the email exists, an OTP has been sent." });
    }

    /// <summary>
    /// Verifies the OTP sent to the user's email address, marking the email as verified.
    /// A verified email is required for certain operations (e.g. password reset).
    /// Returns 400 if the OTP is invalid or expired.
    /// </summary>
    /// <param name="cmd">The email address and the OTP to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { message = "Email verified successfully." });
    }

    /// <summary>
    /// Initiates the password reset flow by sending an OTP to the user's email.
    /// Delegates to <see cref="SendOtp"/> with the "password-reset" purpose.
    /// </summary>
    /// <param name="req">The email address to send the reset OTP to.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
        => await SendOtp(new SendOtpCommand(req.Email, "password-reset"), ct);

    /// <summary>
    /// Resets the user's password after verifying the OTP from the forgot-password flow.
    /// The OTP is validated and the new password is hashed before storage.
    /// Returns 400 if the OTP is invalid, expired, or the new password fails validation.
    /// </summary>
    /// <param name="cmd">The email, OTP, and new password.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { message = "Password reset successfully." });
    }

    // ── Address endpoints ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all saved delivery addresses for the authenticated user.
    /// Addresses are used to pre-fill the delivery address field during checkout.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("addresses")]
    [Authorize]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAddressesQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Saves a new delivery address for the authenticated user.
    /// If this is the user's first address, it is automatically set as the default.
    /// If <c>IsDefault</c> is true, all other addresses are cleared of the default flag.
    /// </summary>
    /// <param name="req">The address details.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Updates an existing saved address. The address must belong to the authenticated
    /// user — the handler enforces ownership via the UserId + AddressId combination.
    /// </summary>
    /// <param name="id">The address to update.</param>
    /// <param name="req">The new address details.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Deletes a saved address. If the deleted address was the default, the next
    /// remaining address is automatically promoted to default so the user always
    /// has a default address if they have any addresses at all.
    /// </summary>
    /// <param name="id">The address to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("addresses/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteAddressCommand(CurrentUserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return NoContent();
    }

    /// <summary>
    /// Sets a specific address as the user's default delivery address.
    /// Clears the default flag from all other addresses atomically before
    /// setting the new default to prevent multiple defaults.
    /// </summary>
    /// <param name="id">The address to set as default.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPatch("addresses/{id:guid}/default")]
    [Authorize]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SetDefaultAddressCommand(CurrentUserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return NoContent();
    }
}

/// <summary>Request body for Google Sign-In, carrying the ID token from the Google SDK.</summary>
public record GoogleAuthRequest(string IdToken);

/// <summary>Request body for the forgot-password flow, carrying the user's email address.</summary>
public record ForgotPasswordRequest(string Email);

/// <summary>
/// Request body for saving or updating a delivery address.
/// <c>Label</c> is a user-friendly name (e.g. "Home", "Office").
/// </summary>
public record AddressRequest(
    string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault);
