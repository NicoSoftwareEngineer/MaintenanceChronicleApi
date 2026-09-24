using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Machines.Queries;
/// <summary>
/// Handler for <see cref="GetMachineByMaintenanceRecordIdQuery"/>.
/// </summary>
public class GetMachineByMaintenanceRecordIdQueryHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository) : IRequestHandler<GetMachineByMaintenanceRecordIdQuery, MachineInMaintenanceRecordDetailDto>
{
    public async Task<MachineInMaintenanceRecordDetailDto> Handle(GetMachineByMaintenanceRecordIdQuery request,
        CancellationToken cancellationToken)
    {
        var specification = new MachineForMaintenanceRecordSpecification(request.RecordId);
        var machine = await machineReadOnlyRepository.GetBySpecificationAsync(specification, cancellationToken);
        if (machine == null)
        {
            throw new BadRequestException(ErrorType.MaintenanceRecordNotFound);
        }

        var machineDto = machine.ToMachineDto();

        return machineDto;
    }
}
