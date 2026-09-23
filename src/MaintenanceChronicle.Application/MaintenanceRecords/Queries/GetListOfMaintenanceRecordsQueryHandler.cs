using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.MaintenanceRecords.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{MaintenanceRecordInListDto}"/> to get list of <see cref="MaintenanceRecordInListDto"/>.
/// </summary>
public class GetListOfMaintenanceRecordsQueryHandler(IReadOnlyRepository<MaintenanceRecord> recordReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<MaintenanceRecordInListDto>, List<MaintenanceRecordInListDto>>
{
    public async Task<List<MaintenanceRecordInListDto>> Handle(GetListOfEntityQuery<MaintenanceRecordInListDto> request, CancellationToken cancellationToken)
    {
        var recordEntities = await recordReadOnlyRepository.ListAsync(cancellationToken, record => record.Machine.Location.Customer);
        var records = recordEntities.Select(record => record.ToListDto()).ToList();

        return records;
    }
}
