using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.LocationContactUsers.Queries;
/// <summary>
/// Handler for <see cref="GetListOfLocationsForContactUserQuery"/>
/// </summary>
public class GetListOfLocationsForContactUserQueryHandler(
    IReadOnlyRepository<User> userReadOnlyRepository,
    IReadOnlyRepository<LocationContactUser> locationContactReadOnlyRepository) : IRequestHandler<GetListOfLocationsForContactUserQuery, List<LocationInListForContactDto>>
{
    public async Task<List<LocationInListForContactDto>> Handle(GetListOfLocationsForContactUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userReadOnlyRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new BadRequestException(ErrorType.UserNotFound);
        }

        var specification = new LocationContactsForUserSpecification(request.UserId);
        var locationContacts = await locationContactReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);
        var locations = locationContacts.Select(contact => contact.ToListForContactDto()).ToList();

        return locations;
    }
}
