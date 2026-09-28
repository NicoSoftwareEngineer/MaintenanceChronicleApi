using MaintenanceChronicle.Application.Contracts.Customers.Queries;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Customers.Queries;
/// <summary>
/// Handler for <see cref="GetCustomerDetailForLocationQuery"/>.
/// </summary>
public class GetCustomerDetailForLocationQueryHandler(IReadOnlyRepository<Customer> customerReadOnlyRepository) : IRequestHandler<GetCustomerDetailForLocationQuery, CustomerDetailForLocationDto>
{
    public async Task<CustomerDetailForLocationDto> Handle(GetCustomerDetailForLocationQuery request,
        CancellationToken cancellationToken)
    {
        var customerForLocationSpecification = new CustomerForLocationSpecification(request.LocationId);
        var customer = await customerReadOnlyRepository.GetBySpecificationAsync(customerForLocationSpecification, cancellationToken);
        if (customer == null)
        {
            throw new BadRequestException(ErrorType.LocationNotFound);
        }

        return customer.ToCustomerDetailForLocationDto();
    }
}
