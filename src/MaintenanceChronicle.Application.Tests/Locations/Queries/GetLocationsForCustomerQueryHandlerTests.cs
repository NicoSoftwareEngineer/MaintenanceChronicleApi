using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Queries;

public class GetLocationsForCustomerQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCustomerLocationsMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme" };
        var query = new GetLocationsForCustomerQuery(customer.Id);
        IReadOnlyList<Location> locations =
        [
            new Location
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Customer = customer,
                Name = "First Location",
                Street = "First Street",
                City = "Prague",
                Country = "Czechia"
            },
            new Location
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Customer = customer,
                Name = "Second Location",
                Street = "Second Street",
                City = "Brno",
                Country = "Czechia"
            }
        ];

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<Location>>(specification => specification is LocationsForCustomerSpecification),
                cancellationToken)
            .Returns(locations);
        var handler = new GetLocationsForCustomerQueryHandler(locationRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = locations.Select(location => new LocationInListDto
        {
            Id = location.Id,
            Name = location.Name,
            Street = location.Street,
            City = location.City,
            Country = location.Country,
            CustomerName = location.Customer.Name
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await locationRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<Location>>(specification => specification is LocationsForCustomerSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenCustomerHasNoLocations()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetLocationsForCustomerQuery(Guid.NewGuid());
        IReadOnlyList<Location> locations = [];

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<Location>>(specification => specification is LocationsForCustomerSpecification),
                cancellationToken)
            .Returns(locations);
        var handler = new GetLocationsForCustomerQueryHandler(locationRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await locationRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<Location>>(specification => specification is LocationsForCustomerSpecification),
            cancellationToken);
    }
}
