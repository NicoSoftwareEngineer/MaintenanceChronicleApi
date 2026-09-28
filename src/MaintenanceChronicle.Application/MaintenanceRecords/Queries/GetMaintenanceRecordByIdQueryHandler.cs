using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.MaintenanceRecords.Queries;
/// <summary>
/// Handler for <see cref="GetEntityByIdQuery{TEntity}"/> for <see cref="MaintenanceRecordDetailDto"/>.
/// </summary>
public class GetMaintenanceRecordByIdQueryHandler(IReadOnlyRepository<MaintenanceRecord> recordReadOnlyRepository)
    : IRequestHandler<GetEntityByIdQuery<MaintenanceRecordDetailDto>, MaintenanceRecordDetailDto>
{
    public async Task<MaintenanceRecordDetailDto> Handle(GetEntityByIdQuery<MaintenanceRecordDetailDto> request,
        CancellationToken cancellationToken)
    {
        MaintenanceRecord? entity = await recordReadOnlyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            throw new BadRequestException(ErrorType.MaintenanceRecordNotFound);
        }

        return entity.ToDetailDto();
    }
}
