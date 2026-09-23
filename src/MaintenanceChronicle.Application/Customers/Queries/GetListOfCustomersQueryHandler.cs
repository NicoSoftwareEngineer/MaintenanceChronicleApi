using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceChronicle.Application.Customers.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{CustomerInListDto}"/> to get list of <see cref="CustomerInListDto"/>.
/// </summary>
public class GetListOfCustomersQueryHandler(IReadOnlyRepository<Customer> customerReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<CustomerInListDto>,List<CustomerInListDto>>
{
    public async Task<List<CustomerInListDto>> Handle(GetListOfEntityQuery<CustomerInListDto> request,
        CancellationToken cancellationToken)
    {
        var customerEntities = await customerReadOnlyRepository.ListAsync(cancellationToken);
        var customers = customerEntities.Select(c => c.ToListDto()).ToList();

        return customers;
    }
}
