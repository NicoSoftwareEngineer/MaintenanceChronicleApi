using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.MaintenanceRecords.Commands;
/// <summary>
/// Handler for <see cref="DeleteEntityByIdCommand{MaintenanceRecord}"/>
/// </summary>
public class DeleteMaintenanceRecordByIdCommandHandler(IRepository<MaintenanceRecord> recordRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<DeleteEntityByIdCommand<MaintenanceRecord>>
{
    public async Task Handle(DeleteEntityByIdCommand<MaintenanceRecord> request, CancellationToken cancellationToken)
    {
        var entity = await recordRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            throw new BadRequestException(ErrorType.MaintenanceRecordNotFound);
        }

        entity.SetDeleteBy(request.UserId, clock.GetCurrentInstant());
        await uow.SaveChangesAsync(cancellationToken);
    }
}
