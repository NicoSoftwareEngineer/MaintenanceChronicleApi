using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.LocationContactUsers.Queries;
/// <summary>
/// Handler for <see cref="GetListOfContactsForLocationQuery"/>
/// </summary>
public class GetListOfContactsForLocationQueryHandler(IReadOnlyRepository<LocationContactUser> contactReadOnlyRepository) : IRequestHandler<GetListOfContactsForLocationQuery, List<LocationContactInListDto>>
{
    public async Task<List<LocationContactInListDto>> Handle(GetListOfContactsForLocationQuery request,
        CancellationToken cancellationToken)
    {
        var specification = new LocationContactsForLocationSpecification(request.LocationId);
        var contacts = await contactReadOnlyRepository.ListBySpecificationAsync(specification, cancellationToken);

        return contacts.Select(contact => contact.ToLocationContactInListDto()).ToList();
    }
}
