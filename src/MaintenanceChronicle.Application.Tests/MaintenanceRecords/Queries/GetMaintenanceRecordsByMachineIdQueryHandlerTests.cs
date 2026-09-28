using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.MaintenanceRecords.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MaintenanceChronicle.Utilities.Enum;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Queries;

public class GetMaintenanceRecordsByMachineIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRecordsForMachine()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var query = new GetMaintenanceRecordsByMachineIdQuery(machine.Id);
        IReadOnlyList<MaintenanceRecord> records =
        [
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(), MachineId = machine.Id, Description = "Installed",
                Date = Instant.FromUtc(2024, 1, 15, 12, 0), Type = RecordType.Installation
            },
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(), MachineId = machine.Id, Description = "Repaired",
                Date = Instant.FromUtc(2025, 3, 20, 12, 0), Type = RecordType.Repair
            }
        ];
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var recordRepository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        recordRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceRecord>>(specification => specification is MaintenanceRecordsForMachineSpecification),
                cancellationToken)
            .Returns(records);
        var handler = new GetMaintenanceRecordsByMachineIdQueryHandler(machineRepository, recordRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = records.Select(record => new MaintenanceRecordInListForMachineDto
        {
            Id = record.Id,
            Type = record.Type.GetTypeName(),
            Date = record.Date.ToString(),
            Description = record.Description
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await machineRepository.Received(1).GetByIdAsync(machine.Id, cancellationToken);
        await recordRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceRecord>>(specification => specification is MaintenanceRecordsForMachineSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenMachineHasNoRecords()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machine = new Machine { Id = Guid.NewGuid() };
        var query = new GetMaintenanceRecordsByMachineIdQuery(machine.Id);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(machine.Id, cancellationToken).Returns(machine);
        var recordRepository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        recordRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<MaintenanceRecord>>(specification => specification is MaintenanceRecordsForMachineSpecification),
                cancellationToken)
            .Returns((IReadOnlyList<MaintenanceRecord>)[]);
        var handler = new GetMaintenanceRecordsByMachineIdQueryHandler(machineRepository, recordRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await recordRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<MaintenanceRecord>>(specification => specification is MaintenanceRecordsForMachineSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetMaintenanceRecordsByMachineIdQuery(Guid.NewGuid());
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.GetByIdAsync(query.MachineId, cancellationToken).Returns((Machine?)null);
        var recordRepository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        var handler = new GetMaintenanceRecordsByMachineIdQueryHandler(machineRepository, recordRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await machineRepository.Received(1).GetByIdAsync(query.MachineId, cancellationToken);
        await recordRepository.DidNotReceive().ListBySpecificationAsync(
            Arg.Any<IListSpecification<MaintenanceRecord>>(), Arg.Any<CancellationToken>());
    }
}
