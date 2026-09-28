using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Customers.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Queries;

public class GetCustomerByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRequestedCustomer_WhenCustomerExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers =
        [
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "First Customer",
                Email = "first@example.com",
                PhoneNumber = "111222333",
                CompanyIdNumber = "10001"
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Second Customer",
                Email = "second@example.com",
                PhoneNumber = "444555666",
                CompanyIdNumber = "20002"
            }
        ];

        var query = new GetEntityByIdQuery<CustomerDetailDto>(customers[1].Id);
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(query.Id, cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Id == query.Id));

        var handler = new GetCustomerByIdQueryHandler(customerRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new CustomerDetailDto
        {
            Id = customers[1].Id,
            Name = customers[1].Name,
            Email = customers[1].Email,
            PhoneNumber = customers[1].PhoneNumber,
            CompanyIdNumber = customers[1].CompanyIdNumber
        };

        result.Should().BeEquivalentTo(expected);
        await customerRepository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsCustomerNotFound_WhenRequestedCustomerIsMissing()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers =
        [
            new Customer { Id = Guid.NewGuid() },
            new Customer { Id = Guid.NewGuid() }
        ];

        var query = new GetEntityByIdQuery<CustomerDetailDto>(Guid.NewGuid());
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(query.Id, cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Id == query.Id));

        var handler = new GetCustomerByIdQueryHandler(customerRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.CustomerNotFound);
        await customerRepository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsCustomerNotFound_WhenThereAreNoCustomers()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers = [];

        var query = new GetEntityByIdQuery<CustomerDetailDto>(Guid.NewGuid());
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(query.Id, cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Id == query.Id));

        var handler = new GetCustomerByIdQueryHandler(customerRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.CustomerNotFound);
        await customerRepository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }
}
