using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.MaintenanceRecords.Commands;
/// <summary>
/// Handler for <see cref="CreateNewMaintenanceRecordCommand"/>
/// </summary>
public class CreateNewMaintenanceRecordCommandHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository, IRepository<MaintenanceRecord> recordRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<CreateNewMaintenanceRecordCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewMaintenanceRecordCommand request, CancellationToken cancellationToken)
    {
        // Check if machine exists
        if (await machineReadOnlyRepository.GetByIdAsync(request.RecordDto.MachineId, cancellationToken) is null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        // Map dto to entity
        var recordEntity = request.RecordDto.ToEntity();
        recordEntity.TenantId = Guid.Parse(request.TenantId);
        recordEntity.SetCreateBy(request.UserId, clock.GetCurrentInstant());
        // add entity to db
        await recordRepository.AddAsync(recordEntity, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return recordEntity.Id;
    }
}
