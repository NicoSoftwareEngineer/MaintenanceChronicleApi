using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceReminders.Commands;

public class MarkMaintenanceReminderAsSentCommandHandlerTests
{
    [Fact]
    public async Task Handle_MarksReminderAsSentAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var reminder = new MaintenanceReminder { Id = Guid.NewGuid(), WasReminderSent = false };
        var command = new MarkMaintenanceReminderAsSentCommand(reminder.Id);
        var repository = Substitute.For<IRepository<MaintenanceReminder>>();
        repository.GetByIdAsync(command.ReminderId, cancellationToken).Returns(reminder);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new MarkMaintenanceReminderAsSentCommandHandler(repository, uow);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        reminder.WasReminderSent.Should().BeTrue();
        await repository.Received(1).GetByIdAsync(command.ReminderId, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenReminderDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new MarkMaintenanceReminderAsSentCommand(Guid.NewGuid());
        var repository = Substitute.For<IRepository<MaintenanceReminder>>();
        repository.GetByIdAsync(command.ReminderId, cancellationToken).Returns((MaintenanceReminder?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new MarkMaintenanceReminderAsSentCommandHandler(repository, uow);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        await repository.Received(1).GetByIdAsync(command.ReminderId, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
