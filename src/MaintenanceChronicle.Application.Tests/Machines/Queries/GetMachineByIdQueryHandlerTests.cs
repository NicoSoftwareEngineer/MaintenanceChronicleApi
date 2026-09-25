using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Machines.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Queries;

public class GetMachineByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRequestedMachine()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var machines = new[]
        {
            new Machine { Id = Guid.NewGuid(), Model = "Other" },
            new Machine
            {
                Id = Guid.NewGuid(), Model = "Pump", Manufacture = "Acme", SerialNumber = "SN-123",
                Color = "Blue", InUseSince = Instant.FromUtc(2024, 1, 15, 12, 0)
            }
        };
        var query = new GetEntityByIdQuery<MachineDetailDto>(machines[1].Id);
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetByIdAsync(query.Id, cancellationToken).Returns(machines[1]);
        var handler = new GetMachineByIdQueryHandler(repository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new MachineDetailDto
        {
            Id = machines[1].Id,
            Model = machines[1].Model,
            Manufacture = machines[1].Manufacture,
            SerialNumber = machines[1].SerialNumber,
            Color = machines[1].Color,
            InUseSince = machines[1].InUseSince
        };
        result.Should().BeEquivalentTo(expected);
        await repository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMachineNotFound_WhenMachineDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetEntityByIdQuery<MachineDetailDto>(Guid.NewGuid());
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetByIdAsync(query.Id, cancellationToken).Returns((Machine?)null);
        var handler = new GetMachineByIdQueryHandler(repository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MachineNotFound);
        await repository.Received(1).GetByIdAsync(query.Id, cancellationToken);
    }
}
