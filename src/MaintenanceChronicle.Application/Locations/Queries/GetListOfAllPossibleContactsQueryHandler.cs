using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.Locations.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{LocationInListDto}"/>.
/// </summary>
public class GetListOfAllPossibleContactsQueryHandler(IReadOnlyRepository<User> userReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<LocationContactInListDto>, List<LocationContactInListDto>>
{
    public async Task<List<LocationContactInListDto>> Handle(GetListOfEntityQuery<LocationContactInListDto> request,
        CancellationToken cancellationToken)
    {
        var users = await userReadOnlyRepository.ListAsync(cancellationToken);
        return users.Select(user => user.ToLocationContactInListDto()).ToList();
    }
}
