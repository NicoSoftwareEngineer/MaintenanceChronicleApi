using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Locations.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Commands;

public class DeleteLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesLocationAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new DeleteEntityByIdCommand<Location>(Guid.NewGuid(), "deleting-user");
        var location = new Location { Id = command.Id };

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetByIdAsync(command.Id, cancellationToken).Returns(location);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new DeleteLocationCommandHandler(locationRepository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        location.DeletedBy.Should().Be(command.UserId);
        location.DeletedAt.Should().Be(now);
        await locationRepository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new DeleteEntityByIdCommand<Location>(Guid.NewGuid(), "deleting-user");

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetByIdAsync(command.Id, cancellationToken).Returns((Location?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new DeleteLocationCommandHandler(locationRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
