namespace Backend.DTOs;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, string ExpiresAt, string Role, string UserId);
public record UserDto(string Id, string Email, string FirstName, string LastName, string Role, string? PhoneNumber);
public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record GoogleAuthRequest(string IdToken);

public record UserAdminDto(string Id, string Email, string FirstName, string LastName, string Role, string? PhoneNumber, bool IsActive, DateTime CreatedAt);
public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? PhoneNumber);
public record ChangeRoleRequest(string Role);
