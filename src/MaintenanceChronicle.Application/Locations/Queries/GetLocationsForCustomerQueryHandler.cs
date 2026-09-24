using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.Locations.Queries;
/// <summary>
/// Handler for <see cref="GetLocationsForCustomerQuery"/>.
/// </summary>
public class GetLocationsForCustomerQueryHandler(IReadOnlyRepository<Location> locationReadOnlyRepository) : IRequestHandler<GetLocationsForCustomerQuery,List<LocationInListDto>>
{
    public async Task<List<LocationInListDto>> Handle(GetLocationsForCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var specification = new LocationsForCustomerSpecification(request.CustomerId);
        var locations = await locationReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return locations.Select(location => location.ToLocationInListDto()).ToList();
    }
}
