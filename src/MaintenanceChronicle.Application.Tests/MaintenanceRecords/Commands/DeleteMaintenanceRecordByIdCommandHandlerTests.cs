using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.MaintenanceRecords.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Commands;

public class DeleteMaintenanceRecordByIdCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesRecordAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new DeleteEntityByIdCommand<MaintenanceRecord>(Guid.NewGuid(), "deleting-user");
        var record = new MaintenanceRecord { Id = command.Id };
        var repository = Substitute.For<IRepository<MaintenanceRecord>>();
        repository.GetByIdAsync(command.Id, cancellationToken).Returns(record);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new DeleteMaintenanceRecordByIdCommandHandler(repository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        record.DeletedBy.Should().Be(command.UserId);
        record.DeletedAt.Should().Be(now);
        await repository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMaintenanceRecordNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new DeleteEntityByIdCommand<MaintenanceRecord>(Guid.NewGuid(), "deleting-user");
        var repository = Substitute.For<IRepository<MaintenanceRecord>>();
        repository.GetByIdAsync(command.Id, cancellationToken).Returns((MaintenanceRecord?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new DeleteMaintenanceRecordByIdCommandHandler(repository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MaintenanceRecordNotFound);
        await repository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
