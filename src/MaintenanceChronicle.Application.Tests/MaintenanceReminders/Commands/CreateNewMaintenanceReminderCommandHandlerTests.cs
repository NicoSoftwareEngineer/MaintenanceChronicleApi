using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands.Dto;
using MaintenanceChronicle.Application.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceReminders.Commands;

public class CreateNewMaintenanceReminderCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsReminderAndSavesChanges_WhenMachineExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var tenantId = Guid.NewGuid();
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new CreateNewMaintenanceReminderCommand(
            new NewMaintenanceReminderDto
            {
                MachineId = machine.Id, Description = "Replace filter", Date = "2026-10-15"
            },
            "creating-user", tenantId.ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var reminderRepository = Substitute.For<IRepository<MaintenanceReminder>>();
        MaintenanceReminder? addedReminder = null;
        reminderRepository.AddAsync(Arg.Any<MaintenanceReminder>(), cancellationToken)
            .Returns(call =>
            {
                var reminder = call.Arg<MaintenanceReminder>();
                addedReminder = reminder;
                return Task.FromResult(reminder);
            });
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new CreateNewMaintenanceReminderCommandHandler(machineRepository, reminderRepository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        addedReminder.Should().NotBeNull();
        addedReminder!.MachineId.Should().Be(machine.Id);
        addedReminder.Description.Should().Be(command.Reminder.Description);
        addedReminder.Date.Should().Be(Instant.FromUtc(2026, 10, 15, 12, 0));
        addedReminder.WasReminderSent.Should().BeFalse();
        addedReminder.TenantId.Should().Be(tenantId);
        addedReminder.CreatedBy.Should().Be(command.UserId);
        addedReminder.CreatedAt.Should().Be(now);
        addedReminder.ModifiedBy.Should().Be(command.UserId);
        addedReminder.ModifiedAt.Should().Be(now);
        await machineRepository.Received(1).GetByIdAsync(machine.Id, cancellationToken);
        await reminderRepository.Received(1).AddAsync(Arg.Any<MaintenanceReminder>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewMaintenanceReminderCommand(
            new NewMaintenanceReminderDto
            {
                MachineId = Guid.NewGuid(), Description = "Replace filter", Date = "2026-10-15"
            },
            "creating-user", Guid.NewGuid().ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(command.Reminder.MachineId, cancellationToken).Returns((Machine?)null);
        var reminderRepository = Substitute.For<IRepository<MaintenanceReminder>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new CreateNewMaintenanceReminderCommandHandler(machineRepository, reminderRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await machineRepository.Received(1).GetByIdAsync(command.Reminder.MachineId, cancellationToken);
        await reminderRepository.DidNotReceive().AddAsync(Arg.Any<MaintenanceReminder>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingReminderFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var command = new CreateNewMaintenanceReminderCommand(
            new NewMaintenanceReminderDto
            {
                MachineId = machine.Id, Description = "Replace filter", Date = "2026-10-15"
            },
            "creating-user", Guid.NewGuid().ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var reminderRepository = Substitute.For<IRepository<MaintenanceReminder>>();
        var failure = new InvalidOperationException("Adding the reminder failed.");
        reminderRepository.AddAsync(Arg.Any<MaintenanceReminder>(), cancellationToken)
            .Returns(_ => Task.FromException<MaintenanceReminder>(failure));
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewMaintenanceReminderCommandHandler(machineRepository, reminderRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await reminderRepository.Received(1).AddAsync(Arg.Any<MaintenanceReminder>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
