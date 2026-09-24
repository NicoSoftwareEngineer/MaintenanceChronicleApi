using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Locations.Queries;
/// <summary>
/// Handler for <see cref="GetLocationForMachineQuery"/>.
/// </summary>
public class GetLocationForMachineQueryHandler(IReadOnlyRepository<Location> locationReadOnlyRepository) : IRequestHandler<GetLocationForMachineQuery, LocationInListDto>
{
    public async Task<LocationInListDto> Handle(GetLocationForMachineQuery request, CancellationToken cancellationToken)
    {
        var specification = new LocationForMachineSpecification(request.MachineId);
        var location = await locationReadOnlyRepository.GetBySpecificationAsync(specification, cancellationToken);
        if (location == null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        return location.ToLocationInListDto();
    }
}
