using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands.Dto;
using MaintenanceChronicle.Application.MaintenanceRecords.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using Microsoft.AspNetCore.JsonPatch;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Commands;

public class UpdateMaintenanceRecordCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesPatchAndSavesChanges_WhenMachineExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var createdAt = Instant.FromUtc(2024, 1, 1, 12, 0);
        var newMachine = new Machine { Id = Guid.NewGuid() };
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), MachineId = Guid.NewGuid(), Description = "Original",
            Date = Instant.FromUtc(2024, 1, 15, 12, 0), Type = RecordType.Installation,
            CreatedAt = createdAt, CreatedBy = "creating-user"
        };
        var patch = new JsonPatchDocument<ManageMaintenanceRecordDetailDto>();
        patch.Replace(dto => dto.Description, "Replaced bearings");
        patch.Replace(dto => dto.Date, "2025-03-20");
        patch.Replace(dto => dto.Type, RecordType.Repair);
        patch.Replace(dto => dto.MachineId, newMachine.Id);
        var command = new UpdateMaintenanceRecordCommand(patch, record.Id, "updating-user");
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        recordRepository.GetByIdAsync(record.Id, cancellationToken).Returns(record);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(newMachine.Id, cancellationToken).Returns(newMachine);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new UpdateMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        var expected = new ManageMaintenanceRecordDetailDto
        {
            Id = record.Id, MachineId = newMachine.Id, Description = "Replaced bearings",
            Date = Instant.FromUtc(2025, 3, 20, 12, 0).ToString(), Type = RecordType.Repair
        };
        result.Should().BeEquivalentTo(expected);
        record.MachineId.Should().Be(newMachine.Id);
        record.Description.Should().Be(expected.Description);
        record.Date.Should().Be(Instant.FromUtc(2025, 3, 20, 12, 0));
        record.Type.Should().Be(expected.Type);
        record.CreatedAt.Should().Be(createdAt);
        record.CreatedBy.Should().Be("creating-user");
        record.ModifiedAt.Should().Be(now);
        record.ModifiedBy.Should().Be(command.UserId);
        await recordRepository.Received(1).GetByIdAsync(record.Id, cancellationToken);
        await machineRepository.Received(1).GetByIdAsync(newMachine.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMaintenanceRecordNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var patch = new JsonPatchDocument<ManageMaintenanceRecordDetailDto>();
        patch.Replace(dto => dto.Description, "Updated");
        var command = new UpdateMaintenanceRecordCommand(patch, Guid.NewGuid(), "updating-user");
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        recordRepository.GetByIdAsync(command.Id, cancellationToken).Returns((MaintenanceRecord?)null);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MaintenanceRecordNotFound);
        await recordRepository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await machineRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WithoutChangingRecord_WhenPatchedMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), MachineId = Guid.NewGuid(), Description = "Original",
            Date = Instant.FromUtc(2024, 1, 15, 12, 0), Type = RecordType.Installation
        };
        var missingMachineId = Guid.NewGuid();
        var patch = new JsonPatchDocument<ManageMaintenanceRecordDetailDto>();
        patch.Replace(dto => dto.Description, "Updated");
        patch.Replace(dto => dto.MachineId, missingMachineId);
        var command = new UpdateMaintenanceRecordCommand(patch, record.Id, "updating-user");
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        recordRepository.GetByIdAsync(record.Id, cancellationToken).Returns(record);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(missingMachineId, cancellationToken).Returns((Machine?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        record.Description.Should().Be("Original");
        record.MachineId.Should().NotBe(missingMachineId);
        await machineRepository.Received(1).GetByIdAsync(missingMachineId, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
