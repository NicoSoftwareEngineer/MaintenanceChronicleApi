using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Application.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using Microsoft.AspNetCore.JsonPatch;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceReminders.Commands;

public class UpdateMaintenanceReminderCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesPatchAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var createdAt = Instant.FromUtc(2025, 1, 1, 12, 0);
        var originalMachineId = Guid.NewGuid();
        var newMachineId = Guid.NewGuid();
        var reminder = new MaintenanceReminder
        {
            Id = Guid.NewGuid(), MachineId = originalMachineId, Description = "Original",
            Date = Instant.FromUtc(2026, 10, 15, 12, 0),
            CreatedAt = createdAt, CreatedBy = "creating-user"
        };
        var patch = new JsonPatchDocument<MaintenanceReminderDetailDto>();
        patch.Replace(dto => dto.Description, "Replace filter");
        patch.Replace(dto => dto.Date, "2026-11-20T12:00:00Z");
        patch.Replace(dto => dto.MachineId, newMachineId);
        var command = new UpdateMaintenanceReminderCommand(reminder.Id, patch, "updating-user");
        var repository = Substitute.For<IRepository<MaintenanceReminder>>();
        repository.GetByIdAsync(reminder.Id, cancellationToken).Returns(reminder);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new UpdateMaintenanceReminderCommandHandler(repository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        var expected = new MaintenanceReminderDetailDto
        {
            Id = reminder.Id, MachineId = newMachineId,
            Description = "Replace filter", Date = "2026-11-20T12:00:00Z"
        };
        result.Should().BeEquivalentTo(expected);
        reminder.Description.Should().Be(expected.Description);
        reminder.Date.Should().Be(Instant.FromUtc(2026, 11, 20, 12, 0));
        reminder.MachineId.Should().Be(newMachineId);
        reminder.CreatedAt.Should().Be(createdAt);
        reminder.CreatedBy.Should().Be("creating-user");
        reminder.ModifiedAt.Should().Be(now);
        reminder.ModifiedBy.Should().Be(command.UserId);
        await repository.Received(1).GetByIdAsync(reminder.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMaintenanceReminderNotFound_WhenReminderDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var patch = new JsonPatchDocument<MaintenanceReminderDetailDto>();
        patch.Replace(dto => dto.Description, "Updated");
        var command = new UpdateMaintenanceReminderCommand(Guid.NewGuid(), patch, "updating-user");
        var repository = Substitute.For<IRepository<MaintenanceReminder>>();
        repository.GetByIdAsync(command.Id, cancellationToken).Returns((MaintenanceReminder?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateMaintenanceReminderCommandHandler(repository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MaintenanceReminderNotFound);
        await repository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
