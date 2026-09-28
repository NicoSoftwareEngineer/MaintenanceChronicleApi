using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Application.Contracts.Locations.Commands.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Locations.Commands;
/// <summary>
/// Handler for <see cref="CreateNewLocationCommand"/>
/// </summary>
public class CreateNewLocationCommandHandler(IReadOnlyRepository<Customer> customerReadOnlyRepository, IRepository<Location> locationRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<CreateNewLocationCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewLocationCommand request, CancellationToken cancellationToken)
    {
        // Check if customer exists
        var customer = await customerReadOnlyRepository.GetByIdAsync(request.LocationDto.CustomerId, cancellationToken);
        if (customer == null) {
            throw new BadRequestException(ErrorType.CustomerNotFound);
        }
        // Create new location
        var location = request.LocationDto.ToEntity();
        location.TenantId = Guid.Parse(request.TenantId);
        location.SetCreateBy(request.UserId, clock.GetCurrentInstant());

        await locationRepository.AddAsync(location, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return location.Id;
    }
}
