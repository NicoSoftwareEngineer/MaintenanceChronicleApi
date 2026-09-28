using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Application.Contracts.Locations.Commands.Dto;
using MaintenanceChronicle.Application.Locations.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using Microsoft.AspNetCore.JsonPatch;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Commands;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesPatchToLocationAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var createdAt = Instant.FromUtc(2026, 1, 1, 12, 0);
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Original Location",
            Street = "Original Street",
            City = "Prague",
            Country = "Czechia",
            CreatedBy = "creating-user",
            CreatedAt = createdAt
        };
        var patch = new JsonPatchDocument<ManageLocationDetailDto>();
        patch.Replace(dto => dto.Name, "Updated Location");
        patch.Replace(dto => dto.Street, "Updated Street");
        var command = new UpdateLocationCommand(patch, location.Id, "updating-user");

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetByIdAsync(command.LocationId, cancellationToken).Returns(location);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new UpdateLocationCommandHandler(locationRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        var expected = new ManageLocationDetailDto
        {
            Id = location.Id,
            Name = "Updated Location",
            Street = "Updated Street",
            City = "Prague",
            Country = "Czechia"
        };
        result.Should().BeEquivalentTo(expected);
        location.Name.Should().Be(expected.Name);
        location.Street.Should().Be(expected.Street);
        location.City.Should().Be(expected.City);
        location.Country.Should().Be(expected.Country);
        location.CreatedBy.Should().Be("creating-user");
        location.CreatedAt.Should().Be(createdAt);
        location.ModifiedBy.Should().Be(command.UserId);
        location.ModifiedAt.Should().Be(now);
        await locationRepository.Received(1).GetByIdAsync(command.LocationId, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var patch = new JsonPatchDocument<ManageLocationDetailDto>();
        patch.Replace(dto => dto.Name, "Updated Location");
        var command = new UpdateLocationCommand(patch, Guid.NewGuid(), "updating-user");

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetByIdAsync(command.LocationId, cancellationToken).Returns((Location?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateLocationCommandHandler(locationRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetByIdAsync(command.LocationId, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
