using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Queries;

public class GetLocationByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRequestedLocation_WhenLocationExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Workshop",
            Street = "Main Street",
            City = "Prague",
            Country = "Czechia"
        };
        var query = new GetEntityByIdQuery<LocationDetailDto>(location.Id);

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(query.Id, cancellationToken).Returns(location);
        var handler = new GetLocationByIdQueryHandler(locationRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new LocationDetailDto
        {
            Id = location.Id,
            Name = location.Name,
            Street = location.Street,
            City = location.City,
            Country = location.Country
        };
        result.Should().BeEquivalentTo(expected);
        await locationRepository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetEntityByIdQuery<LocationDetailDto>(Guid.NewGuid());

        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(query.Id, cancellationToken).Returns((Location?)null);
        var handler = new GetLocationByIdQueryHandler(locationRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }
}
