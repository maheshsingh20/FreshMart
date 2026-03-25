using AuthService.Application.Commands;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.CQRS;

namespace AuthService.Application.Queries;

public record GetAddressesQuery(Guid UserId) : IQuery<List<AddressDto>>;

public class GetAddressesHandler(AuthDbContext db) : IQueryHandler<GetAddressesQuery, List<AddressDto>>
{
    public async Task<List<AddressDto>> Handle(GetAddressesQuery q, CancellationToken ct) =>
        await db.Addresses
            .Where(a => a.UserId == q.UserId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new AddressDto(a.Id, a.Label, a.Line1, a.Line2,
                a.City, a.State, a.Pincode, a.Country, a.IsDefault))
            .ToListAsync(ct);
}
