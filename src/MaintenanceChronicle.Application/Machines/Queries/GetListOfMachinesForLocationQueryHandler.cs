using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Machines.Queries;
/// <summary>
/// Handler for <see cref="GetMachinesForLocationQuery"/>.
/// </summary>
public class GetMachinesForLocationQueryHandler(
    IReadOnlyRepository<Location> locationReadOnlyRepository,
    IReadOnlyRepository<Machine> machineReadOnlyRepository) : IRequestHandler<GetMachinesForLocationQuery, List<MachineInListForLocationDto>>
{
    public async Task<List<MachineInListForLocationDto>> Handle(GetMachinesForLocationQuery request, CancellationToken cancellationToken)
    {
        var location = await locationReadOnlyRepository.GetByIdAsync(request.LocationId, cancellationToken);
        if (location == null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        var specification = new MachinesForLocationSpecification(request.LocationId);
        var machines = await machineReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return machines.Select(machine => machine.ToMachineInListForLocationDto()).ToList();
    }
}
