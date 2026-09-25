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

public class GetAllMaintenanceRemindersForMachineQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRemindersForMachine()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetAllMaintenanceRemindersForMachineQuery(Guid.NewGuid());
        IReadOnlyList<MaintenanceReminder> reminders =
        [
            new MaintenanceReminder
            {
                Id = Guid.NewGuid(), MachineId = query.MachineId, Description = "Inspect pump",
                Date = Instant.FromUtc(2026, 9, 25, 12, 0)
            },
            new MaintenanceReminder
            {
                Id = Guid.NewGuid(), MachineId = query.MachineId, Description = "Replace filter",
                Date = Instant.FromUtc(2026, 10, 25, 12, 0)
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceReminder>>();
        repository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is MaintenanceRemindersForMachineSpecification),
                cancellationToken)
            .Returns(reminders);
        var handler = new GetAllMaintenanceRemindersForMachineQueryHandler(repository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = reminders.Select(reminder => new MaintenanceReminderInListForMachineDto
        {
            Id = reminder.Id,
            Description = reminder.Description,
            Date = reminder.Date
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await repository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is MaintenanceRemindersForMachineSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenMachineHasNoReminders()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetAllMaintenanceRemindersForMachineQuery(Guid.NewGuid());
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceReminder>>();
        repository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is MaintenanceRemindersForMachineSpecification),
                cancellationToken)
            .Returns((IReadOnlyList<MaintenanceReminder>)[]);
        var handler = new GetAllMaintenanceRemindersForMachineQueryHandler(repository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await repository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceReminder>>(specification => specification is MaintenanceRemindersForMachineSpecification),
            cancellationToken);
    }
}
