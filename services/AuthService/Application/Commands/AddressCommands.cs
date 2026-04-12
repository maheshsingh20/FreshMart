using AuthService.Domain;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Read model DTO representing a saved delivery address.
/// Returned by address queries and command responses so the frontend can
/// update its address list without a separate GET request.
/// </summary>
public record AddressDto(Guid Id, string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault);

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>
/// Command to create a new address or update an existing one.
/// When <c>AddressId</c> is null, a new address is created.
/// When <c>AddressId</c> is provided, the existing address is updated.
/// This dual-purpose design reduces the number of command types while keeping
/// the handler logic straightforward.
/// </summary>
public record SaveAddressCommand(
    Guid UserId, string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault,
    Guid? AddressId = null) : ICommand<AddressDto>;

/// <summary>
/// Command to delete a saved address. Ownership is enforced by requiring both
/// the user ID and the address ID, preventing users from deleting other users' addresses.
/// </summary>
public record DeleteAddressCommand(Guid UserId, Guid AddressId) : ICommand;

/// <summary>
/// Command to set a specific address as the user's default delivery address.
/// All other addresses for the user are cleared of the default flag atomically.
/// </summary>
public record SetDefaultAddressCommand(Guid UserId, Guid AddressId) : ICommand;

// ── Handlers ──────────────────────────────────────────────────────────────────

/// <summary>
/// Handles <see cref="SaveAddressCommand"/> for both create and update operations.
/// When creating, if this is the user's first address it is automatically set as
/// default regardless of the <c>IsDefault</c> flag, ensuring every user always
/// has a default address once they add one.
/// When setting as default, all existing defaults are cleared first using a
/// bulk update to avoid loading all addresses into memory.
/// </summary>
public class SaveAddressHandler(AuthDbContext db) : ICommandHandler<SaveAddressCommand, AddressDto>
{
    /// <summary>
    /// Creates or updates an address, managing the default flag atomically.
    /// </summary>
    /// <returns>
    /// <see cref="Result{T}.Success"/> with the saved <see cref="AddressDto"/>.
    /// Throws if the address to update is not found (should not happen in normal flow).
    /// </returns>
    public async Task<Result<AddressDto>> Handle(SaveAddressCommand cmd, CancellationToken ct)
    {
        // If setting as default, clear existing defaults
        if (cmd.IsDefault)
            await db.Addresses.Where(a => a.UserId == cmd.UserId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);

        Address address;
        if (cmd.AddressId.HasValue)
        {
            // Edit existing
            address = await db.Addresses.FirstOrDefaultAsync(
                a => a.Id == cmd.AddressId && a.UserId == cmd.UserId, ct)
                ?? throw new Exception("Address not found");
            address.Update(cmd.Label, cmd.Line1, cmd.Line2, cmd.City, cmd.State, cmd.Pincode, cmd.Country);
            if (cmd.IsDefault) address.SetDefault(true);
        }
        else
        {
            // Create new — if first address, make it default
            var count = await db.Addresses.CountAsync(a => a.UserId == cmd.UserId, ct);
            address = Address.Create(cmd.UserId, cmd.Label, cmd.Line1, cmd.Line2,
                cmd.City, cmd.State, cmd.Pincode, cmd.Country, cmd.IsDefault || count == 0);
            db.Addresses.Add(address);
        }

        await db.SaveChangesAsync(ct);
        return Result<AddressDto>.Success(ToDto(address));
    }

    /// <summary>Maps an <see cref="Address"/> domain entity to an <see cref="AddressDto"/>.</summary>
    public static AddressDto ToDto(Address a) =>
        new(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State, a.Pincode, a.Country, a.IsDefault);
}

/// <summary>
/// Handles <see cref="DeleteAddressCommand"/> by removing the address and
/// automatically promoting the next remaining address to default if the
/// deleted address was the default. This ensures the user always has a
/// default address as long as they have any addresses.
/// </summary>
public class DeleteAddressHandler(AuthDbContext db) : ICommandHandler<DeleteAddressCommand>
{
    /// <summary>
    /// Deletes the address and promotes a new default if necessary.
    /// </summary>
    /// <returns>
    /// <see cref="Result.Success"/> on success.
    /// <see cref="Result.Failure"/> if the address is not found or does not belong to the user.
    /// </returns>
    public async Task<Result> Handle(DeleteAddressCommand cmd, CancellationToken ct)
    {
        var address = await db.Addresses.FirstOrDefaultAsync(
            a => a.Id == cmd.AddressId && a.UserId == cmd.UserId, ct);
        if (address is null) return Result.Failure("Address not found");

        db.Addresses.Remove(address);
        await db.SaveChangesAsync(ct);

        // If deleted was default, promote first remaining
        if (address.IsDefault)
        {
            var next = await db.Addresses.FirstOrDefaultAsync(a => a.UserId == cmd.UserId, ct);
            if (next is not null) { next.SetDefault(true); await db.SaveChangesAsync(ct); }
        }
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="SetDefaultAddressCommand"/> by atomically clearing all
/// existing defaults for the user and setting the specified address as default.
/// Uses a bulk update (<c>ExecuteUpdateAsync</c>) to clear defaults without
/// loading all addresses into memory, then loads only the target address to set it.
/// </summary>
public class SetDefaultAddressHandler(AuthDbContext db) : ICommandHandler<SetDefaultAddressCommand>
{
    /// <summary>
    /// Clears existing defaults and sets the new default address.
    /// </summary>
    /// <returns>
    /// <see cref="Result.Success"/> on success.
    /// <see cref="Result.Failure"/> if the address is not found or does not belong to the user.
    /// </returns>
    public async Task<Result> Handle(SetDefaultAddressCommand cmd, CancellationToken ct)
    {
        await db.Addresses.Where(a => a.UserId == cmd.UserId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);

        var address = await db.Addresses.FirstOrDefaultAsync(
            a => a.Id == cmd.AddressId && a.UserId == cmd.UserId, ct);
        if (address is null) return Result.Failure("Address not found");

        address.SetDefault(true);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
