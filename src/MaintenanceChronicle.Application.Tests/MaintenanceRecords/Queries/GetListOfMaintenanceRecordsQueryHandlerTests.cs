using System.Linq.Expressions;
using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.MaintenanceRecords.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Enum;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Queries;

public class GetListOfMaintenanceRecordsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRecordsMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<MaintenanceRecord> records =
        [
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(), Description = "Installed", Date = Instant.FromUtc(2024, 1, 15, 12, 0),
                Type = RecordType.Installation,
                Machine = new Machine
                {
                    Model = "Pump", SerialNumber = "SN-1",
                    Location = new Location { Name = "Workshop", Customer = new Customer { Name = "Acme" } }
                }
            },
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(), Description = "Repaired", Date = Instant.FromUtc(2025, 3, 20, 12, 0),
                Type = RecordType.Repair,
                Machine = new Machine
                {
                    Model = "Compressor", SerialNumber = "SN-2",
                    Location = new Location { Name = "Warehouse", Customer = new Customer { Name = "Contoso" } }
                }
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        repository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1))
            .Returns(records);
        var handler = new GetListOfMaintenanceRecordsQueryHandler(repository);

        // Act
        var result = await handler.Handle(new GetListOfEntityQuery<MaintenanceRecordInListDto>(), cancellationToken);

        // Assert
        var expected = records.Select(record => new MaintenanceRecordInListDto
        {
            Id = record.Id,
            LocationName = record.Machine.Location.Name,
            MachineName = record.Machine.Model,
            MachineSerialNumber = record.Machine.SerialNumber,
            Type = record.Type.GetTypeName(),
            Date = record.Date.ToString(),
            Description = record.Description,
            CustomerName = record.Machine.Location.Customer.Name
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await repository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenThereAreNoRecords()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        repository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1))
            .Returns((IReadOnlyList<MaintenanceRecord>)[]);
        var handler = new GetListOfMaintenanceRecordsQueryHandler(repository);

        // Act
        var result = await handler.Handle(new GetListOfEntityQuery<MaintenanceRecordInListDto>(), cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await repository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_RequestsOneExtraRecordWithRelatedData_WhenPageIsSpecified()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<MaintenanceRecord> records =
        [
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                Description = "Installed",
                Date = Instant.FromUtc(2024, 1, 15, 12, 0),
                Type = RecordType.Installation,
                Machine = new Machine
                {
                    Model = "Pump",
                    SerialNumber = "SN-1",
                    Location = new Location { Name = "Workshop", Customer = new Customer { Name = "Acme" } }
                }
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        repository.ListPageAsync(5, 6, cancellationToken,
                Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1))
            .Returns(records);
        var handler = new GetListOfMaintenanceRecordsQueryHandler(repository);
        var query = new GetListOfEntityQuery<MaintenanceRecordInListDto>(new PageRequest(2, 5));

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new MaintenanceRecordInListDto
        {
            Id = records[0].Id,
            LocationName = records[0].Machine.Location.Name,
            MachineName = records[0].Machine.Model,
            MachineSerialNumber = records[0].Machine.SerialNumber,
            Type = records[0].Type.GetTypeName(),
            Date = records[0].Date.ToString(),
            Description = records[0].Description,
            CustomerName = records[0].Machine.Location.Customer.Name
        });
        await repository.Received(1).ListPageAsync(5, 6, cancellationToken,
            Arg.Is<Expression<Func<MaintenanceRecord, object>>[]>(includes => includes.Length == 1));
        await repository.DidNotReceive().ListAsync(cancellationToken,
            Arg.Any<Expression<Func<MaintenanceRecord, object>>[]>());
    }
}
