using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries.Dto;
using MaintenanceChronicle.Application.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceReminders.Commands;

[CollectionDefinition("Reminder email template", DisableParallelization = true)]
public class ReminderEmailTemplateCollection;

[Collection("Reminder email template")]
public class GenerateMaintenanceReminderEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_GeneratesEmailFromTemplateForLocationContacts()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var reminder = new DueMaintenanceReminderDto
        {
            Id = Guid.NewGuid(), MachineId = Guid.NewGuid(), Description = "Replace filter",
            Date = Instant.FromUtc(2026, 10, 15, 12, 0)
        };
        var command = new GenerateMaintenanceReminderEmailCommand(reminder);
        var firstUser = new User
        {
            Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith", Email = "alice@example.com"
        };
        var secondUser = new User
        {
            Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Jones", Email = "bob@example.com"
        };
        var machine = new Machine
        {
            Id = reminder.MachineId, Manufacture = "Acme", Model = "Pump", SerialNumber = "SN-123",
            Location = new Location
            {
                Name = "Workshop", Street = "Main Street", City = "Prague", Country = "Czechia",
                Contacts =
                [
                    new LocationContactUser { User = firstUser },
                    new LocationContactUser { User = secondUser }
                ]
            }
        };
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Machine>>(specification => specification is MachineWithLocationContactsSpecification),
                cancellationToken)
            .Returns(machine);
        var handler = new GenerateMaintenanceReminderEmailCommandHandler(repository);
        var originalDirectory = Directory.GetCurrentDirectory();
        var templateDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "MaintenanceChronicle.Utilities"));
        File.Exists(Path.Combine(templateDirectory, "EmailTemplates", "MaintenanceReminderEmail.html"))
            .Should().BeTrue();

        // Act
        try
        {
            Directory.SetCurrentDirectory(templateDirectory);
            var result = await handler.Handle(command, cancellationToken);

            // Assert
            result.Subject.Should().Be("Maintenance Reminder");
            result.Recipients.Should().BeEquivalentTo(new Dictionary<string, string?>
            {
                [firstUser.Email] = "Alice Smith",
                [secondUser.Email] = "Bob Jones"
            });
            result.Body.Should().Contain("Acme Pump");
            result.Body.Should().Contain(machine.SerialNumber);
            result.Body.Should().Contain("2026-10-15");
            result.Body.Should().Contain(reminder.Description);
            result.Body.Should().Contain(machine.Location.Name);
            result.Body.Should().Contain("Main Street, Prague, Czechia");
            result.Body.Should().NotContain("[MachineName]");
            await repository.Received(1).GetBySpecificationAsync(
                Arg.Is<ISpecification<Machine>>(specification => specification is MachineWithLocationContactsSpecification),
                cancellationToken);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new GenerateMaintenanceReminderEmailCommand(new DueMaintenanceReminderDto
        {
            MachineId = Guid.NewGuid(), Description = "Replace filter",
            Date = Instant.FromUtc(2026, 10, 15, 12, 0)
        });
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Machine>>(specification => specification is MachineWithLocationContactsSpecification),
                cancellationToken)
            .Returns((Machine?)null);
        var handler = new GenerateMaintenanceReminderEmailCommandHandler(repository);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await repository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Machine>>(specification => specification is MachineWithLocationContactsSpecification),
            cancellationToken);
    }
}
