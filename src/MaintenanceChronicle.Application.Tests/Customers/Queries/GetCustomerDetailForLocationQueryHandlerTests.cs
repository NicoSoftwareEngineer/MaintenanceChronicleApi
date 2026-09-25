using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Customers.Queries;
using MaintenanceChronicle.Application.Contracts.Customers.Queries.Dto;
using MaintenanceChronicle.Application.Customers.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Queries;

public class GetCustomerDetailForLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCustomerForRequestedLocation_WhenLocationExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var locationId = Guid.NewGuid();
        IReadOnlyList<Customer> customers =
        [
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "First Customer",
                Email = "first@example.com",
                PhoneNumber = "111222333",
                CompanyIdNumber = "10001",
                Locations = [new Location { Id = Guid.NewGuid() }]
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Second Customer",
                Email = "second@example.com",
                PhoneNumber = "444555666",
                CompanyIdNumber = "20002",
                Locations = [new Location { Id = locationId }]
            }
        ];

        var query = new GetCustomerDetailForLocationQuery(locationId);
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer?>>();
        customerRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Locations.Any(location => location.Id == query.LocationId)));

        var handler = new GetCustomerDetailForLocationQueryHandler(customerRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new CustomerDetailForLocationDto
        {
            Id = customers[1].Id,
            Name = customers[1].Name,
            Email = customers[1].Email,
            PhoneNumber = customers[1].PhoneNumber,
            CompanyIdNumber = customers[1].CompanyIdNumber
        };

        result.Should().BeEquivalentTo(expected);
        await customerRepository.Received(1)
            .GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenNoCustomerHasRequestedLocation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers =
        [
            new Customer { Id = Guid.NewGuid(), Locations = [new Location { Id = Guid.NewGuid() }] },
            new Customer { Id = Guid.NewGuid(), Locations = [new Location { Id = Guid.NewGuid() }] }
        ];

        var query = new GetCustomerDetailForLocationQuery(Guid.NewGuid());
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer?>>();
        customerRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Locations.Any(location => location.Id == query.LocationId)));

        var handler = new GetCustomerDetailForLocationQueryHandler(customerRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await customerRepository.Received(1)
            .GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenThereAreNoCustomers()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Customer> customers = [];

        var query = new GetCustomerDetailForLocationQuery(Guid.NewGuid());
        var customerRepository = Substitute.For<IReadOnlyRepository<Customer?>>();
        customerRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken)
            .Returns(customers.SingleOrDefault(customer => customer.Locations.Any(location => location.Id == query.LocationId)));

        var handler = new GetCustomerDetailForLocationQueryHandler(customerRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await customerRepository.Received(1)
            .GetBySpecificationAsync(
                Arg.Is<ISpecification<Customer?>>(specification => specification is CustomerForLocationSpecification),
                cancellationToken);
    }
}
