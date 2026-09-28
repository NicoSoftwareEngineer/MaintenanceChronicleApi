using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.Customers.Queries;
/// <summary>
/// Handler for <see cref="GetEntityByIdQuery{CustomerDetailDto}"/> to get a customer by ID.
/// </summary>
public class GetCustomerByIdQueryHandler(IReadOnlyRepository<Customer> customerReadOnlyRepository) : IRequestHandler<GetEntityByIdQuery<CustomerDetailDto>, CustomerDetailDto>
{
    public async Task<CustomerDetailDto> Handle(GetEntityByIdQuery<CustomerDetailDto> request,
        CancellationToken cancellationToken)
    {
        var customer = await customerReadOnlyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
        {
            throw new BadRequestException(ErrorType.CustomerNotFound);
        }

        var customerDetailDto = customer.ToCustomerDetailDto();

        return customerDetailDto;
    }
}
