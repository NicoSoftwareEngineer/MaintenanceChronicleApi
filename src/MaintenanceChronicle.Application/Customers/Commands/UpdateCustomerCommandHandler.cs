using MaintenanceChronicle.Application.Contracts.Customers.Commands;
using MaintenanceChronicle.Application.Contracts.Customers.Commands.Dto;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Interfaces;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MaintenanceChronicle.Application.Customers.Commands;
/// <summary>
/// Handler for <see cref="UpdateCustomerCommand"/>
/// </summary>
public class UpdateCustomerCommandHandler(IReadOnlyRepository<Customer> customerReadOnlyRepository, IUnitOfWork uow, IClock clock) : IRequestHandler<UpdateCustomerCommand, ManageCustomerDetailDto>
{
    public async Task<ManageCustomerDetailDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Get customer entity from database
        var customerEntity = await customerReadOnlyRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customerEntity == null)
        {
            throw new BadRequestException(ErrorType.CustomerNotFound);
        }

        // Map entity to Dto and apply patch
        var customerMapped = customerEntity.ToManageCustomerDetailDto();
        request.Patch.ApplyTo(customerMapped);

        // Map changed props back to entity
        customerMapped.MapToEntity(customerEntity);

        customerEntity.SetModifyBy(request.UserId, clock.GetCurrentInstant());

        await uow.SaveChangesAsync(cancellationToken);
        
        return customerMapped;
    }
}
