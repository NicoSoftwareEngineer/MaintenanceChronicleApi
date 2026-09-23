using MaintenanceChronicle.Application.Contracts.Customers.Commands;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Customers.Commands;
/// <summary>
/// Handler for <see cref="CreateNewCustomerCommand"/>
/// </summary>
public class CreateNewCustomerCommandHandler(IRepository<Customer> customerRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<CreateNewCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewCustomerCommand request, CancellationToken cancellationToken)
    {
        var customerDto = request.NewCustomer;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = customerDto.Name,
            Email = customerDto.Email,
            CompanyIdNumber = customerDto.CompanyIdNumber,
            PhoneNumber = customerDto.PhoneNumber,
            TenantId = Guid.Parse(request.TenantId)
        };
        customer.SetCreateBy(request.UserId, clock.GetCurrentInstant());

        await customerRepository.AddAsync(customer, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
