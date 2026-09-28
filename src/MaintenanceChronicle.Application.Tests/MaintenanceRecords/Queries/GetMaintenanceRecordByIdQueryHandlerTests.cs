using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.MaintenanceRecords.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.MaintenanceRecords.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MaintenanceChronicle.Utilities.Enum;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.MaintenanceRecords.Queries;

public class GetMaintenanceRecordByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRequestedRecord()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var records = new[]
        {
            new MaintenanceRecord { Id = Guid.NewGuid(), Description = "Other" },
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(), Description = "Replaced bearings",
                Date = Instant.FromUtc(2025, 3, 20, 12, 0), Type = RecordType.Repair
            }
        };
        var query = new GetEntityByIdQuery<MaintenanceRecordDetailDto>(records[1].Id);
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        repository.GetByIdAsync(query.Id, cancellationToken).Returns(records[1]);
        var handler = new GetMaintenanceRecordByIdQueryHandler(repository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new MaintenanceRecordDetailDto
        {
            Id = records[1].Id,
            Description = records[1].Description,
            Date = records[1].Date,
            Type = records[1].Type.GetTypeName()
        };
        result.Should().BeEquivalentTo(expected);
        await repository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMaintenanceRecordNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetEntityByIdQuery<MaintenanceRecordDetailDto>(Guid.NewGuid());
        var repository = Substitute.For<IReadOnlyRepository<MaintenanceRecord>>();
        repository.GetByIdAsync(query.Id, cancellationToken).Returns((MaintenanceRecord?)null);
        var handler = new GetMaintenanceRecordByIdQueryHandler(repository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MaintenanceRecordNotFound);
        await repository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }
}
