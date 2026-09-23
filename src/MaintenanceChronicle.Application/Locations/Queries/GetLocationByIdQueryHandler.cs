using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Locations.Queries;
/// <summary>
/// Handler for <see cref="GetEntityByIdQuery{TEntity}"/> to get a location by ID.
/// </summary>
public class GetLocationByIdQueryHandler(IReadOnlyRepository<Location> locationReadOnlyRepository) : IRequestHandler<GetEntityByIdQuery<LocationDetailDto>, LocationDetailDto>
{
    public async Task<LocationDetailDto> Handle(GetEntityByIdQuery<LocationDetailDto> request,
        CancellationToken cancellationToken)
    {
        var location = await locationReadOnlyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (location == null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        var locationDetailDto = location.ToLocationDetailDto();

        return locationDetailDto;
    }
}
