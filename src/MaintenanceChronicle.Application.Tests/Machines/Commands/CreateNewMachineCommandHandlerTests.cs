using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Commands;
using MaintenanceChronicle.Application.Contracts.Machines.Commands.Dto;
using MaintenanceChronicle.Application.Machines.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Commands;

public class CreateNewMachineCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsMachineAndSavesChanges_WhenLocationExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var location = new Location { Id = Guid.NewGuid() };
        var tenantId = Guid.NewGuid();
        var generatedId = Guid.NewGuid();
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new CreateNewMachineCommand(
            new NewMachineDto
            {
                LocationId = location.Id, Model = "Pump", Manufacture = "Acme",
                SerialNumber = "SN-123", Color = "Blue", InUseSince = "2024-01-15"
            },
            "creating-user", tenantId.ToString());
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(location.Id, cancellationToken).Returns(location);
        var machineRepository = Substitute.For<IRepository<Machine>>();
        Machine? addedMachine = null;
        machineRepository.AddAsync(Arg.Any<Machine>(), cancellationToken)
            .Returns(call =>
            {
                var machine = call.Arg<Machine>();
                machine.Id = generatedId;
                addedMachine = machine;
                return Task.FromResult(machine);
            });
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new CreateNewMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        result.Should().Be(generatedId);
        addedMachine.Should().NotBeNull();
        addedMachine!.LocationId.Should().Be(location.Id);
        addedMachine.Model.Should().Be(command.NewMachineDto.Model);
        addedMachine.Manufacture.Should().Be(command.NewMachineDto.Manufacture);
        addedMachine.SerialNumber.Should().Be(command.NewMachineDto.SerialNumber);
        addedMachine.Color.Should().Be(command.NewMachineDto.Color);
        addedMachine.InUseSince.Should().Be(Instant.FromUtc(2024, 1, 15, 12, 0));
        addedMachine.TenantId.Should().Be(tenantId);
        addedMachine.CreatedBy.Should().Be(command.UserId);
        addedMachine.CreatedAt.Should().Be(now);
        addedMachine.ModifiedBy.Should().Be(command.UserId);
        addedMachine.ModifiedAt.Should().Be(now);
        await locationRepository.Received(1).GetByIdAsync(location.Id, cancellationToken);
        await machineRepository.Received(1).AddAsync(Arg.Any<Machine>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewMachineCommand(
            new NewMachineDto
            {
                LocationId = Guid.NewGuid(), Model = "Pump", Manufacture = "Acme",
                SerialNumber = "SN-123", Color = "Blue", InUseSince = "2024-01-15"
            },
            "creating-user", Guid.NewGuid().ToString());
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(command.NewMachineDto.LocationId, cancellationToken).Returns((Location?)null);
        var machineRepository = Substitute.For<IRepository<Machine>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new CreateNewMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetByIdAsync(command.NewMachineDto.LocationId, cancellationToken);
        await machineRepository.DidNotReceive().AddAsync(Arg.Any<Machine>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingMachineFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var location = new Location { Id = Guid.NewGuid() };
        var command = new CreateNewMachineCommand(
            new NewMachineDto
            {
                LocationId = location.Id, Model = "Pump", Manufacture = "Acme",
                SerialNumber = "SN-123", Color = "Blue", InUseSince = "2024-01-15"
            },
            "creating-user", Guid.NewGuid().ToString());
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(location.Id, cancellationToken).Returns(location);
        var machineRepository = Substitute.For<IRepository<Machine>>();
        var failure = new InvalidOperationException("Adding the machine failed.");
        machineRepository.AddAsync(Arg.Any<Machine>(), cancellationToken)
            .Returns(_ => Task.FromException<Machine>(failure));
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await machineRepository.Received(1).AddAsync(Arg.Any<Machine>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
