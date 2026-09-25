using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Commands.Dto;
using MaintenanceChronicle.Application.MaintenanceRecords.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Commands;

public class CreateNewMaintenanceRecordCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsRecordAndSavesChanges_WhenMachineExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var tenantId = Guid.NewGuid();
        var generatedId = Guid.NewGuid();
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new CreateNewMaintenanceRecordCommand(
            new NewMaintenanceRecordDto
            {
                MachineId = machine.Id, Description = "Replaced bearings",
                Date = "2025-03-20", RecordType = (int)RecordType.Repair
            },
            "creating-user", tenantId.ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        MaintenanceRecord? addedRecord = null;
        recordRepository.AddAsync(Arg.Any<MaintenanceRecord>(), cancellationToken)
            .Returns(call =>
            {
                var record = call.Arg<MaintenanceRecord>();
                record.Id = generatedId;
                addedRecord = record;
                return Task.FromResult(record);
            });
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new CreateNewMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        result.Should().Be(generatedId);
        addedRecord.Should().NotBeNull();
        addedRecord!.MachineId.Should().Be(machine.Id);
        addedRecord.Description.Should().Be(command.RecordDto.Description);
        addedRecord.Date.Should().Be(Instant.FromUtc(2025, 3, 20, 12, 0));
        addedRecord.Type.Should().Be(RecordType.Repair);
        addedRecord.TenantId.Should().Be(tenantId);
        addedRecord.CreatedBy.Should().Be(command.UserId);
        addedRecord.CreatedAt.Should().Be(now);
        addedRecord.ModifiedBy.Should().Be(command.UserId);
        addedRecord.ModifiedAt.Should().Be(now);
        await machineRepository.Received(1).GetByIdAsync(machine.Id, cancellationToken);
        await recordRepository.Received(1).AddAsync(Arg.Any<MaintenanceRecord>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewMaintenanceRecordCommand(
            new NewMaintenanceRecordDto
            {
                MachineId = Guid.NewGuid(), Description = "Replaced bearings",
                Date = "2025-03-20", RecordType = (int)RecordType.Repair
            },
            "creating-user", Guid.NewGuid().ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(command.RecordDto.MachineId, cancellationToken).Returns((Machine?)null);
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new CreateNewMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await machineRepository.Received(1).GetByIdAsync(command.RecordDto.MachineId, cancellationToken);
        await recordRepository.DidNotReceive().AddAsync(Arg.Any<MaintenanceRecord>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingRecordFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var command = new CreateNewMaintenanceRecordCommand(
            new NewMaintenanceRecordDto
            {
                MachineId = machine.Id, Description = "Replaced bearings",
                Date = "2025-03-20", RecordType = (int)RecordType.Repair
            },
            "creating-user", Guid.NewGuid().ToString());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var recordRepository = Substitute.For<IRepository<MaintenanceRecord>>();
        var failure = new InvalidOperationException("Adding the record failed.");
        recordRepository.AddAsync(Arg.Any<MaintenanceRecord>(), cancellationToken)
            .Returns(_ => Task.FromException<MaintenanceRecord>(failure));
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewMaintenanceRecordCommandHandler(machineRepository, recordRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await recordRepository.Received(1).AddAsync(Arg.Any<MaintenanceRecord>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
