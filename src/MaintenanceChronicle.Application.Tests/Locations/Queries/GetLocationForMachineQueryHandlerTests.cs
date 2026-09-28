using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Queries;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Queries;

public class GetLocationForMachineQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLocationForRequestedMachine()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetLocationForMachineQuery(Guid.NewGuid());
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Workshop",
            Street = "Main Street",
            City = "Prague",
            Country = "Czechia",
            Customer = new Customer { Name = "Acme" }
        };

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Location>>(specification => specification is LocationForMachineSpecification),
                cancellationToken)
            .Returns(location);
        var handler = new GetLocationForMachineQueryHandler(locationRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new LocationInListDto
        {
            Id = location.Id,
            Name = location.Name,
            Street = location.Street,
            City = location.City,
            Country = location.Country,
            CustomerName = location.Customer.Name
        };
        result.Should().BeEquivalentTo(expected);
        await locationRepository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Location>>(specification => specification is LocationForMachineSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenRepositoryReturnsNoLocation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetLocationForMachineQuery(Guid.NewGuid());

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Location>>(specification => specification is LocationForMachineSpecification),
                cancellationToken)
            .Returns((Location?)null);
        var handler = new GetLocationForMachineQueryHandler(locationRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await locationRepository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Location>>(specification => specification is LocationForMachineSpecification),
            cancellationToken);
    }
}
