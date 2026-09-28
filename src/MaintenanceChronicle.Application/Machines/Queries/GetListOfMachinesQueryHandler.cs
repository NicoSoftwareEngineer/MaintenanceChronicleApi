using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.Machines.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{MachineInListDto}"/> to get list of <see cref="MachineInListDto"/>
/// </summary>
public class GetListOfMachinesQueryHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<MachineInListDto>, List<MachineInListDto>>
{
    public async Task<List<MachineInListDto>> Handle(GetListOfEntityQuery<MachineInListDto> request,
        CancellationToken cancellationToken)
    {
        var machineEntities = request.PageRequest is { } pageRequest
            ? await machineReadOnlyRepository.ListPageAsync(
                checked((pageRequest.Page - 1) * pageRequest.PageSize),
                pageRequest.PageSize + 1,
                cancellationToken,
                machine => machine.Location.Customer)
            : await machineReadOnlyRepository.ListAsync(cancellationToken, machine => machine.Location.Customer);
        var machines = machineEntities.Select(machine => machine.ToMachineInListDto()).ToList();

        return machines;
    }
}
