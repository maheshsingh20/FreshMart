using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AppDbContext db, JwtService jwt, IHttpClientFactory httpClientFactory, EmailService emailService, IConfiguration config) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    private static string GenerateOtp() => Random.Shared.Next(100000, 999999).ToString();

    // ── POST /api/v1/auth/register ────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return Conflict(new { error = "Email already registered" });

        // Determine role — default Customer, elevated roles require invite code
        var role = "Customer";
        var allowedRoles = new[] { "StoreManager", "DeliveryDriver", "Admin" };
        if (!string.IsNullOrWhiteSpace(req.Role) && allowedRoles.Contains(req.Role))
        {
            var expectedCode = config[$"InviteCodes:{req.Role}"];
            if (string.IsNullOrWhiteSpace(req.InviteCode) || req.InviteCode != expectedCode)
                return BadRequest(new { error = "Invalid invite code for the selected role." });
            role = req.Role;
        }

        var otp = GenerateOtp();
        var user = new AppUser
        {
            Email = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            PhoneNumber = req.PhoneNumber,
            Role = role,
            EmailVerified = false,
            EmailOtp = otp,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        _ = emailService.SendEmailVerificationOtpAsync(user.Email, user.FirstName, otp);
        return Ok(new { message = "OTP sent to your email. Please verify to continue.", email = user.Email });
    }

    // ── POST /api/v1/auth/verify-email ────────────────────────────────────────
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null) return NotFound(new { error = "User not found" });
        if (user.EmailVerified) return BadRequest(new { error = "Email already verified" });
        if (user.EmailOtp != req.Otp || user.OtpExpiry < DateTime.UtcNow)
            return BadRequest(new { error = "Invalid or expired OTP" });

        user.EmailVerified = true;
        user.EmailOtp = null;
        user.OtpExpiry = null;

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        _ = emailService.SendWelcomeAsync(user.Email, user.FirstName);
        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }

    // ── POST /api/v1/auth/resend-otp ─────────────────────────────────────────
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null) return NotFound(new { error = "User not found" });
        if (user.EmailVerified) return BadRequest(new { error = "Email already verified" });

        var otp = GenerateOtp();
        user.EmailOtp = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync();

        _ = emailService.SendEmailVerificationOtpAsync(user.Email, user.FirstName, otp);
        return Ok(new { message = "OTP resent successfully" });
    }

    // ── POST /api/v1/auth/login ───────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        if (!user.EmailVerified && user.GoogleId == null)
            return Unauthorized(new { error = "Please verify your email before logging in.", code = "EMAIL_NOT_VERIFIED", email = user.Email });

        if (!user.IsActive)
            return Unauthorized(new { error = "Your account has been deactivated. Contact support." });

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }

    // ── POST /api/v1/auth/forgot-password ────────────────────────────────────
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        // Always return OK to prevent email enumeration
        if (user == null || user.GoogleId != null)
            return Ok(new { message = "If that email exists, a reset OTP has been sent." });

        var otp = GenerateOtp();
        user.PasswordResetOtp = otp;
        user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync();

        _ = emailService.SendPasswordResetOtpAsync(user.Email, user.FirstName, otp);
        return Ok(new { message = "If that email exists, a reset OTP has been sent." });
    }

    // ── POST /api/v1/auth/reset-password ─────────────────────────────────────
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null) return BadRequest(new { error = "Invalid request" });
        if (user.PasswordResetOtp != req.Otp || user.PasswordResetOtpExpiry < DateTime.UtcNow)
            return BadRequest(new { error = "Invalid or expired OTP" });
        if (req.NewPassword.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.PasswordResetOtp = null;
        user.PasswordResetOtpExpiry = null;
        user.RefreshToken = null; // invalidate all sessions
        await db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully. Please log in." });
    }

    // ── POST /api/v1/auth/refresh ─────────────────────────────────────────────
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);
        if (user == null) return Unauthorized(new { error = "Invalid or expired refresh token" });

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }

    // ── POST /api/v1/auth/logout ──────────────────────────────────────────────
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user != null) { user.RefreshToken = null; await db.SaveChangesAsync(); }
        return NoContent();
    }

    // ── GET /api/v1/auth/me ───────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id.ToString(), user.Email, user.FirstName, user.LastName, user.Role, user.PhoneNumber));
    }

    // ── PUT /api/v1/auth/me ───────────────────────────────────────────────────
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.PhoneNumber = req.PhoneNumber;
        await db.SaveChangesAsync();
        var accessToken = jwt.GenerateAccessToken(user);
        return Ok(new { user = new UserDto(user.Id.ToString(), user.Email, user.FirstName, user.LastName, user.Role, user.PhoneNumber), accessToken });
    }

    // ── POST /api/v1/auth/change-password ────────────────────────────────────
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Current password is incorrect" });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── POST /api/v1/auth/google ──────────────────────────────────────────────
    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth(GoogleAuthRequest req)
    {
        var client = httpClientFactory.CreateClient();
        var resp = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={req.IdToken}");
        if (!resp.IsSuccessStatusCode)
            return Unauthorized(new { error = "Invalid Google token" });

        var json = await resp.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(json);

        var googleId = payload.GetProperty("sub").GetString()!;
        var userEmail = payload.GetProperty("email").GetString()!.ToLower();
        var given  = payload.TryGetProperty("given_name",  out var gn) ? gn.GetString() ?? "" : "";
        var family = payload.TryGetProperty("family_name", out var fn) ? fn.GetString() ?? "" : "";

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId || u.Email == userEmail);
        if (user == null)
        {
            user = new AppUser
            {
                Email = userEmail,
                FirstName = given,
                LastName = family,
                GoogleId = googleId,
                EmailVerified = true, // Google accounts are pre-verified
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
            };
            db.Users.Add(user);
            _ = emailService.SendWelcomeAsync(user.Email, user.FirstName);
        }
        else
        {
            if (user.GoogleId == null) user.GoogleId = googleId;
            user.EmailVerified = true;
        }

        var accessToken  = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }
}
