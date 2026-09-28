using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Customers.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Queries;

public class GetListOfCustomersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCustomersMappedToListDtos()
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

        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.ListAsync(cancellationToken).Returns(customers);

        var handler = new GetListOfCustomersQueryHandler(customerRepository);
        var query = new GetListOfEntityQuery<CustomerInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new[]
        {
            new CustomerInListDto
            {
                Id = customers[0].Id,
                Name = customers[0].Name,
                Email = customers[0].Email,
                PhoneNumber = customers[0].PhoneNumber,
                CompanyIdNumber = customers[0].CompanyIdNumber
            },
            new CustomerInListDto
            {
                Id = customers[1].Id,
                Name = customers[1].Name,
                Email = customers[1].Email,
                PhoneNumber = customers[1].PhoneNumber,
                CompanyIdNumber = customers[1].CompanyIdNumber
            }
        };

        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await customerRepository.Received(1).ListAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenRepositoryReturnsNoCustomers()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers = [];

        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.ListAsync(cancellationToken).Returns(customers);

        var handler = new GetListOfCustomersQueryHandler(customerRepository);
        var query = new GetListOfEntityQuery<CustomerInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await customerRepository.Received(1).ListAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_RequestsOneExtraCustomer_WhenPageIsSpecified()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers =
        [
            new Customer { Id = Guid.NewGuid(), Name = "Paged Customer" }
        ];
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.ListPageAsync(5, 6, cancellationToken).Returns(customers);
        var handler = new GetListOfCustomersQueryHandler(customerRepository);
        var query = new GetListOfEntityQuery<CustomerInListDto>(new PageRequest(2, 5));

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().ContainSingle()
            .Which.Id.Should().Be(customers[0].Id);
        await customerRepository.Received(1).ListPageAsync(5, 6, cancellationToken);
        await customerRepository.DidNotReceive().ListAsync(cancellationToken);
    }
}
