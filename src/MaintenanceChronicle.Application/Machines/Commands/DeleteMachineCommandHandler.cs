using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Machines.Commands;
/// <summary>
/// Handler for <see cref="DeleteEntityByIdCommand{Machine}"/>
/// </summary>
public class DeleteMachineCommandHandler(IRepository<Machine> machineRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<DeleteEntityByIdCommand<Machine>>
{
    public async Task Handle(DeleteEntityByIdCommand<Machine> request, CancellationToken cancellationToken)
    {
        var machine = await machineRepository.GetByIdAsync(request.Id, cancellationToken);
        if (machine is null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }
        machine.SetDeleteBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);
    }
}
