using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Machines.Queries;
/// <summary>
/// Handler for <see cref="GetMachineByIdQuery"/>.
/// </summary>
public class GetMachineByIdQueryHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository) : IRequestHandler<GetEntityByIdQuery<MachineDetailDto>, MachineDetailDto>
{
    public async Task<MachineDetailDto> Handle(GetEntityByIdQuery<MachineDetailDto> request,
        CancellationToken cancellationToken)
    {
        var machine = await machineReadOnlyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (machine is null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        return machine.ToMachineDetailDto();
    }
}
