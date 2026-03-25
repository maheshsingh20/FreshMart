using AuthService.Domain;
using FluentValidation;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid UserId = default) : ICommand<UserProfileResponse>;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
    }
}

public class UpdateProfileHandler(IUserRepository repo)
    : ICommandHandler<UpdateProfileCommand, UserProfileResponse>
{
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
