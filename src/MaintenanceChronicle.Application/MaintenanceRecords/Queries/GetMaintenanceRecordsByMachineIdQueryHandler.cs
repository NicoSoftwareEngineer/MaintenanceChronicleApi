using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.MaintenanceRecords.Queries;
/// <summary>
/// Handler for <see cref="GetMaintenanceRecordsByMachineIdQuery"/>.
/// </summary>
public class GetMaintenanceRecordsByMachineIdQueryHandler(
    IReadOnlyRepository<Machine> machineReadOnlyRepository,
    IReadOnlyRepository<MaintenanceRecord> recordReadOnlyRepository) : IRequestHandler<GetMaintenanceRecordsByMachineIdQuery, List<MaintenanceRecordInListForMachineDto>>
{
    public async Task<List<MaintenanceRecordInListForMachineDto>> Handle(GetMaintenanceRecordsByMachineIdQuery request,
        CancellationToken cancellationToken)
    {
        var machine = await machineReadOnlyRepository.GetByIdAsync(request.MachineId, cancellationToken);
        if (machine == null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        var specification = new MaintenanceRecordsForMachineSpecification(request.MachineId);
        var records = await recordReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return records.Select(record => record.ToListForMachineDto()).ToList();
    }
}
