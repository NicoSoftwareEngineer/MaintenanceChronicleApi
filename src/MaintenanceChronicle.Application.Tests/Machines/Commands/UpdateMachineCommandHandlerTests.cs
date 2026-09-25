using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Commands;
using MaintenanceChronicle.Application.Contracts.Machines.Commands.Dto;
using MaintenanceChronicle.Application.Machines.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using Microsoft.AspNetCore.JsonPatch;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Commands;

public class UpdateMachineCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesPatchAndSavesChanges_WhenLocationExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var createdAt = Instant.FromUtc(2024, 1, 1, 12, 0);
        var oldLocationId = Guid.NewGuid();
        var newLocation = new Location { Id = Guid.NewGuid() };
        var machine = new Machine
        {
            Id = Guid.NewGuid(), Model = "Original", Manufacture = "Acme", SerialNumber = "SN-123",
            Color = "Blue", InUseSince = Instant.FromUtc(2024, 1, 15, 12, 0),
            LocationId = oldLocationId, CreatedAt = createdAt, CreatedBy = "creating-user"
        };
        var patch = new JsonPatchDocument<ManageMachineDetailDto>();
        patch.Replace(dto => dto.Model, "Updated");
        patch.Replace(dto => dto.Color, "Red");
        patch.Replace(dto => dto.InUseSince, "2025-03-20");
        patch.Replace(dto => dto.LocationId, newLocation.Id);
        var command = new UpdateMachineCommand(patch, machine.Id, "updating-user");
        var machineRepository = Substitute.For<IRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(newLocation.Id, cancellationToken).Returns(newLocation);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new UpdateMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        var expected = new ManageMachineDetailDto
        {
            Id = machine.Id, Model = "Updated", Manufacture = machine.Manufacture,
            SerialNumber = machine.SerialNumber, Color = "Red",
            InUseSince = "2025-03-20", LocationId = newLocation.Id
        };
        result.Should().BeEquivalentTo(expected);
        machine.Model.Should().Be(expected.Model);
        machine.Manufacture.Should().Be(expected.Manufacture);
        machine.SerialNumber.Should().Be(expected.SerialNumber);
        machine.Color.Should().Be(expected.Color);
        machine.InUseSince.Should().Be(Instant.FromUtc(2025, 3, 20, 12, 0));
        machine.LocationId.Should().Be(newLocation.Id);
        machine.CreatedAt.Should().Be(createdAt);
        machine.CreatedBy.Should().Be("creating-user");
        machine.ModifiedAt.Should().Be(now);
        machine.ModifiedBy.Should().Be(command.UserId);
        await machineRepository.Received(1).GetByIdAsync(machine.Id, cancellationToken);
        await locationRepository.Received(1).GetByIdAsync(newLocation.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var patch = new JsonPatchDocument<ManageMachineDetailDto>();
        patch.Replace(dto => dto.Model, "Updated");
        var command = new UpdateMachineCommand(patch, Guid.NewGuid(), "updating-user");
        var machineRepository = Substitute.For<IRepository<Machine>>();
        machineRepository.GetByIdAsync(command.MachineId, cancellationToken).Returns((Machine?)null);
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await machineRepository.Received(1).GetByIdAsync(command.MachineId, cancellationToken);
        await locationRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WithoutChangingMachine_WhenPatchedLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine
        {
            Id = Guid.NewGuid(), Model = "Original", Manufacture = "Acme", SerialNumber = "SN-123",
            Color = "Blue", InUseSince = Instant.FromUtc(2024, 1, 15, 12, 0), LocationId = Guid.NewGuid()
        };
        var missingLocationId = Guid.NewGuid();
        var patch = new JsonPatchDocument<ManageMachineDetailDto>();
        patch.Replace(dto => dto.Model, "Updated");
        patch.Replace(dto => dto.LocationId, missingLocationId);
        var command = new UpdateMachineCommand(patch, machine.Id, "updating-user");
        var machineRepository = Substitute.For<IRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(missingLocationId, cancellationToken).Returns((Location?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateMachineCommandHandler(locationRepository, machineRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        machine.Model.Should().Be("Original");
        machine.LocationId.Should().NotBe(missingLocationId);
        await locationRepository.Received(1).GetByIdAsync(missingLocationId, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
