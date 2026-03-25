using AuthService.Domain;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.CQRS;
using SharedKernel.Domain;

namespace AuthService.Application.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record AddressDto(Guid Id, string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault);

// ── Commands ──────────────────────────────────────────────────────────────────
public record SaveAddressCommand(
    Guid UserId, string Label, string Line1, string? Line2,
    string City, string State, string Pincode, string Country, bool IsDefault,
    Guid? AddressId = null) : ICommand<AddressDto>;

public record DeleteAddressCommand(Guid UserId, Guid AddressId) : ICommand;
public record SetDefaultAddressCommand(Guid UserId, Guid AddressId) : ICommand;

// ── Handlers ──────────────────────────────────────────────────────────────────
public class SaveAddressHandler(AuthDbContext db) : ICommandHandler<SaveAddressCommand, AddressDto>
{
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

    public static AddressDto ToDto(Address a) =>
        new(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State, a.Pincode, a.Country, a.IsDefault);
}

public class DeleteAddressHandler(AuthDbContext db) : ICommandHandler<DeleteAddressCommand>
{
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

public class SetDefaultAddressHandler(AuthDbContext db) : ICommandHandler<SetDefaultAddressCommand>
{
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
