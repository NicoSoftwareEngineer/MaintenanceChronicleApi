using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.Locations.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{LocationInListDto}"/> to get list of <see cref="LocationInListDto"/>.
/// </summary>
public class GetListOfLocationsQueryHandler(IReadOnlyRepository<Location> locationReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<LocationInListDto>, List<LocationInListDto>>
{
    public async Task<List<LocationInListDto>> Handle(GetListOfEntityQuery<LocationInListDto> request,
        CancellationToken cancellationToken)
    {
        var locationEntities = request.PageRequest is { } pageRequest
            ? await locationReadOnlyRepository.ListPageAsync(
                checked((pageRequest.Page - 1) * pageRequest.PageSize),
                pageRequest.PageSize + 1,
                cancellationToken,
                location => location.Customer)
            : await locationReadOnlyRepository.ListAsync(cancellationToken, location => location.Customer);
        var locationList = locationEntities.Select(location => location.ToLocationInListDto()).ToList();

        return locationList;
    }
}
