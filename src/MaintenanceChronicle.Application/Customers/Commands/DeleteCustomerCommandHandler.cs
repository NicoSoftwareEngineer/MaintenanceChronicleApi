using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime;

namespace MaintenanceChronicle.Application.Customers.Commands;
/// <summary>
/// Handler for <see cref="DeleteEntityByIdCommand{Customer}"/>
/// </summary>
public class DeleteCustomerCommandHandler(IRepository<Customer> customerRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<DeleteEntityByIdCommand<Customer>>
{
    public async Task Handle(DeleteEntityByIdCommand<Customer> request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            throw new BadRequestException(ErrorType.CustomerNotFound);
        }

        customer.SetDeleteBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);
    }
}
