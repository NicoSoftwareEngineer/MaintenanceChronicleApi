using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Application.MaintenanceReminders.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceReminders.Queries;

public class GetAllDueMaintenanceRemindersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDueRemindersMappedToDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        IReadOnlyList<MaintenanceReminder> reminders =
        [
            new MaintenanceReminder
            {
                Id = Guid.NewGuid(), MachineId = Guid.NewGuid(), Description = "Inspect pump",
                Date = Instant.FromUtc(2026, 9, 24, 12, 0), WasReminderSent = false
            },
            new MaintenanceReminder
            {
                Id = Guid.NewGuid(), MachineId = Guid.NewGuid(), Description = "Replace filter",
                Date = now, WasReminderSent = true
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceReminder>>();
        repository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is DueMaintenanceRemindersSpecification),
                cancellationToken)
            .Returns(reminders);
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new GetAllDueMaintenanceRemindersQueryHandler(repository, clock);

        // Act
        var result = await handler.Handle(new GetAllDueMaintenanceRemindersQuery(), cancellationToken);

        // Assert
        var expected = reminders.Select(reminder => new DueMaintenanceReminderDto
        {
            Id = reminder.Id,
            Description = reminder.Description,
            Date = reminder.Date,
            MachineId = reminder.MachineId,
            WasSent = reminder.WasReminderSent
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        clock.Received(1).GetCurrentInstant();
        await repository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is DueMaintenanceRemindersSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoRemindersAreDue()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceReminder>>();
        repository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is DueMaintenanceRemindersSpecification),
                cancellationToken)
            .Returns((IReadOnlyList<MaintenanceReminder>)[]);
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new GetAllDueMaintenanceRemindersQueryHandler(repository, clock);

        // Act
        var result = await handler.Handle(new GetAllDueMaintenanceRemindersQuery(), cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await repository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is DueMaintenanceRemindersSpecification),
            cancellationToken);
    }
}
