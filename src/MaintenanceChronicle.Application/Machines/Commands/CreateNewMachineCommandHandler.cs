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
/// Handler for <see cref="CreateNewMachineCommand"/>
/// </summary>
public class CreateNewMachineCommandHandler(IReadOnlyRepository<Location> locationReadOnlyRepository, IRepository<Machine> machineRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<CreateNewMachineCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewMachineCommand request, CancellationToken cancellationToken)
    {
        if (await locationReadOnlyRepository.GetByIdAsync(request.NewMachineDto.LocationId, cancellationToken) is null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        var machineEntity = request.NewMachineDto.ToMachineEntity();
        machineEntity.TenantId = Guid.Parse(request.TenantId);
        machineEntity.SetCreateBy(request.UserId, clock.GetCurrentInstant());

        await machineRepository.AddAsync(machineEntity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return machineEntity.Id;
    }
}
