using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Locations.Commands;
/// <summary>
/// Handler for <see cref="ManageContactsInLocationCommand"/>.
/// </summary>
public class ManageContactsInLocationCommandHandler(
    IRepository<Location> locationRepository,
    IReadOnlyRepository<User> userReadOnlyRepository,
    IRepository<LocationContactUser> locationContactRepository,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ManageContactsInLocationCommand>
{
    public async Task Handle(ManageContactsInLocationCommand request, CancellationToken cancellationToken)
    {
        // Get current location from db
        var locationSpecification = new LocationWithContactsSpecification(request.LocationId);
        var location = await locationRepository.GetBySpecificationAsync(locationSpecification, cancellationToken);
        if (location == null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        var currentInstant = clock.GetCurrentInstant();
        // Get all contact ids from request
        var contactIds = request.Contacts.Select(c => c.Id).ToList();

        foreach (var existingContacts in location.Contacts)
        {
            //Remove existing contact if it is not in requests contact list
            if (contactIds.All(x => x != existingContacts.UserId))
            {
                existingContacts.SetDeleteBy(request.UserId, currentInstant);
            }
        }

        foreach (var contact in request.Contacts)
        {
            //Add contact if it is not in existing contact list
            if (location.Contacts.All(x => x.UserId != contact.Id))
            {
                var user = await userReadOnlyRepository.GetByIdAsync(contact.Id, cancellationToken);
                if (user == null)
                {
                    throw new BadRequestException(ErrorType.UserNotFound);
                }

                var locationContactUser = new LocationContactUser
                {
                    UserId = user.Id,
                    LocationId = location.Id,
                    TenantId = Guid.Parse(request.TenantId)
                };
                locationContactUser.SetCreateBy(request.UserId, currentInstant);

                await locationContactRepository.AddAsync(locationContactUser, cancellationToken);
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
    }
}
