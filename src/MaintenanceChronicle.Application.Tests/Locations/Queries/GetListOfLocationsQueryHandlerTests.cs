using System.Linq.Expressions;
using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Queries;

public class GetListOfLocationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLocationsMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme" };
        IReadOnlyList<Location> locations =
        [
            new Location
            {
                Id = Guid.NewGuid(),
                Name = "First Location",
                Street = "First Street",
                City = "Prague",
                Country = "Czechia",
                Customer = customer
            },
            new Location
            {
                Id = Guid.NewGuid(),
                Name = "Second Location",
                Street = "Second Street",
                City = "Brno",
                Country = "Czechia",
                Customer = customer
            }
        ];

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1))
            .Returns(locations);
        var handler = new GetListOfLocationsQueryHandler(locationRepository);
        var query = new GetListOfEntityQuery<LocationInListDto>();

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
        await locationRepository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenRepositoryReturnsNoLocations()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Location> locations = [];

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1))
            .Returns(locations);
        var handler = new GetListOfLocationsQueryHandler(locationRepository);
        var query = new GetListOfEntityQuery<LocationInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await locationRepository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_RequestsOneExtraLocationWithCustomer_WhenPageIsSpecified()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Acme" };
        IReadOnlyList<Location> locations =
        [
            new Location { Id = Guid.NewGuid(), Name = "Paged Location", Customer = customer }
        ];
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.ListPageAsync(5, 6, cancellationToken,
                Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1))
            .Returns(locations);
        var handler = new GetListOfLocationsQueryHandler(locationRepository);
        var query = new GetListOfEntityQuery<LocationInListDto>(new PageRequest(2, 5));

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new LocationInListDto
        {
            Id = locations[0].Id,
            Name = locations[0].Name,
            Street = locations[0].Street,
            City = locations[0].City,
            Country = locations[0].Country,
            CustomerName = customer.Name
        });
        await locationRepository.Received(1).ListPageAsync(5, 6, cancellationToken,
            Arg.Is<Expression<Func<Location, object>>[]>(includes => includes.Length == 1));
        await locationRepository.DidNotReceive().ListAsync(cancellationToken,
            Arg.Any<Expression<Func<Location, object>>[]>());
    }
}
