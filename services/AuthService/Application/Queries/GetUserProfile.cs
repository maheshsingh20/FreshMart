using AuthService.Domain;
using SharedKernel.CQRS;

namespace AuthService.Application.Commands;

public record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileResponse>;
public record UserProfileResponse(Guid Id, string Email, string FirstName, string LastName,
    string Role, string? PhoneNumber, bool IsEmailVerified);

public record RevokeTokenCommand(string RefreshToken) : ICommand;

public class GetUserProfileHandler(IUserRepository repo)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserProfileResponse(user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), user.PhoneNumber, user.IsEmailVerified);
    }
}

public class RevokeTokenHandler(IUserRepository repo) : ICommandHandler<RevokeTokenCommand>
{
    public async Task<SharedKernel.Domain.Result> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByRefreshTokenAsync(cmd.RefreshToken, ct);
        if (user is null) return SharedKernel.Domain.Result.Failure("Token not found.");
        user.RevokeRefreshToken();
        await repo.UpdateAsync(user, ct);
        return SharedKernel.Domain.Result.Success();
    }
}
