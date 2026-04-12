using AuthService.Domain;
using FluentValidation;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

/// <summary>
/// Command to update the authenticated user's profile information.
/// <c>UserId</c> defaults to <see cref="Guid.Empty"/> and is overwritten by the
/// controller with the value from the JWT claim, preventing users from updating
/// other accounts by supplying a different ID in the request body.
/// </summary>
public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid UserId = default) : ICommand<UserProfileResponse>;

/// <summary>
/// FluentValidation rules for <see cref="UpdateProfileCommand"/>.
/// Enforces non-empty names with reasonable length limits and an optional
/// phone number length constraint.
/// </summary>
public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
    }
}

/// <summary>
/// Handles <see cref="UpdateProfileCommand"/> by loading the user, applying the
/// profile changes via the domain method, and persisting the updated entity.
/// Returns the updated profile as a <see cref="UserProfileResponse"/> so the
/// frontend can update its user context without a separate GET request.
/// </summary>
public class UpdateProfileHandler(IUserRepository repo)
    : ICommandHandler<UpdateProfileCommand, UserProfileResponse>
{
    /// <summary>
    /// Loads the user, delegates the update to the domain model's
    /// <c>UpdateProfile</c> method (which may enforce additional invariants),
    /// persists the change, and returns the updated profile.
    /// </summary>
    /// <returns>
    /// <see cref="Result{T}.Success"/> with the updated profile, or
    /// <see cref="Result{T}.Failure"/> if the user is not found.
    /// </returns>
    public async Task<Result<UserProfileResponse>> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(cmd.UserId, ct);
        if (user is null)
            return Result<UserProfileResponse>.Failure("User not found.");

        user.UpdateProfile(cmd.FirstName, cmd.LastName, cmd.PhoneNumber);
        await repo.UpdateAsync(user, ct);

        return Result<UserProfileResponse>.Success(
            new UserProfileResponse(user.Id, user.Email, user.FirstName, user.LastName,
                user.Role.ToString(), user.PhoneNumber, user.IsEmailVerified));
    }
}
