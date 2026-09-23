using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Locations.Commands;
/// <summary>
/// Handler for <see cref="DeleteEntityByIdCommand{Location}"/>
/// </summary>
public class DeleteLocationCommandHandler(IReadOnlyRepository<Location> locationReadOnlyRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<DeleteEntityByIdCommand<Location>>
{
    public async Task Handle(DeleteEntityByIdCommand<Location> request, CancellationToken cancellationToken)
    {
        var location = await locationReadOnlyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (location == null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        location.SetDeleteBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);
    }
}
