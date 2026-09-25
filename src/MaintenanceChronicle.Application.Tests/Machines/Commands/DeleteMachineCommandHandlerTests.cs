using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Machines.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Commands;

public class DeleteMachineCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesMachineAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new DeleteEntityByIdCommand<Machine>(Guid.NewGuid(), "deleting-user");
        var machine = new Machine { Id = command.Id };
        var repository = Substitute.For<IRepository<Machine>>();
        repository.GetByIdAsync(command.Id, cancellationToken).Returns(machine);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new DeleteMachineCommandHandler(repository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        machine.DeletedBy.Should().Be(command.UserId);
        machine.DeletedAt.Should().Be(now);
        await repository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new DeleteEntityByIdCommand<Machine>(Guid.NewGuid(), "deleting-user");
        var repository = Substitute.For<IRepository<Machine>>();
        repository.GetByIdAsync(command.Id, cancellationToken).Returns((Machine?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new DeleteMachineCommandHandler(repository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await repository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
