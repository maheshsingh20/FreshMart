using AuthService.Domain;
using FluentValidation;
using MediatR;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserRole Role = UserRole.Customer) : ICommand<RegisterUserResponse>;

public record RegisterUserResponse(Guid UserId, string Email, string Role);

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain uppercase.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class RegisterUserHandler(IUserRepository repo, IPasswordHasher hasher)
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand cmd, CancellationToken ct)
    {
        if (await repo.ExistsAsync(cmd.Email, ct))
            return Result<RegisterUserResponse>.Failure("Email already registered.");

        var hash = hasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, hash, cmd.FirstName, cmd.LastName, cmd.Role, cmd.PhoneNumber);

        await repo.AddAsync(user, ct);
        return Result<RegisterUserResponse>.Success(
            new RegisterUserResponse(user.Id, user.Email, user.Role.ToString()));
    }
}
