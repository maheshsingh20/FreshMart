using AuthService.Application.Services;
using AuthService.Domain;
using AuthService.Infrastructure;
using FluentValidation;
using MediatR;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

// ── Send OTP (email verification or password reset) ──────────────────────────

public record SendOtpCommand(string Email, string Purpose) : ICommand;
// Purpose: "email-verification" | "password-reset"

public class SendOtpValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Purpose).Must(p => p is "email-verification" or "password-reset");
    }
}

public class SendOtpHandler(
    IUserRepository repo,
    INotificationRelay relay) : ICommandHandler<SendOtpCommand>
{
    public async Task<Result> Handle(SendOtpCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByEmailAsync(cmd.Email, ct);
        if (user == null) return Result.Success(); // silent — don't leak existence

        var otp = user.GenerateOtp(cmd.Purpose);
        await repo.UpdateAsync(user, ct);
        _ = relay.NotifyOtpAsync(user.Id, user.Email, user.FirstName, otp, cmd.Purpose, ct);
        return Result.Success();
    }
}

// ── Verify Email ──────────────────────────────────────────────────────────────

public record VerifyEmailCommand(string Email, string Otp) : ICommand;

public class VerifyEmailHandler(IUserRepository repo) : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByEmailAsync(cmd.Email, ct);
        if (user == null) return Result.Failure("Invalid OTP.");

        if (!user.ValidateOtp(cmd.Otp, "email-verification"))
            return Result.Failure("Invalid or expired OTP.");

        user.VerifyEmail();
        await repo.UpdateAsync(user, ct);
        return Result.Success();
    }
}

// ── Reset Password ────────────────────────────────────────────────────────────

public record ResetPasswordCommand(string Email, string Otp, string NewPassword) : ICommand;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain uppercase.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public class ResetPasswordHandler(
    IUserRepository repo,
    IPasswordHasher hasher) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByEmailAsync(cmd.Email, ct);
        if (user == null) return Result.Failure("Invalid OTP.");

        if (!user.ValidateOtp(cmd.Otp, "password-reset"))
            return Result.Failure("Invalid or expired OTP.");

        user.ResetPassword(hasher.Hash(cmd.NewPassword));
        await repo.UpdateAsync(user, ct);
        return Result.Success();
    }
}
