using MaintenanceChronicle.Application.Contracts.Machines.Commands;
using MaintenanceChronicle.Application.Contracts.Machines.Commands.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Machines.Commands;
/// <summary>
/// Handler for <see cref="UpdateMachineCommand"/>
/// </summary>
public class UpdateMachineCommandHandler(IReadOnlyRepository<Location> locationReadOnlyRepository, IRepository<Machine> machineRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<UpdateMachineCommand,ManageMachineDetailDto>
{
    public async Task<ManageMachineDetailDto> Handle(UpdateMachineCommand request, CancellationToken cancellationToken)
    {
        // Get machine from db
        var machineEntity = await machineRepository.GetByIdAsync(request.MachineId, cancellationToken);
        if (machineEntity is null)
        {
           throw new BadRequestException(ErrorType.MachineNotFound);
        }
        // Map to dto
        var machineDetail = machineEntity.ToManageMachineDetailDto();
        // Apply patch to dto
        request.Patch.ApplyTo(machineDetail);

        if (await locationReadOnlyRepository.GetByIdAsync(machineDetail.LocationId, cancellationToken) is null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }
        // Map changed properties back to entity
        machineDetail.MapToEntity(machineEntity);
        machineEntity.SetModifyBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);

        return machineDetail;
    }
}
