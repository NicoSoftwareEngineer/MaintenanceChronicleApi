using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Machines.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Queries;

public class GetMachineByMaintenanceRecordIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMachineForMaintenanceRecord()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetMachineByMaintenanceRecordIdQuery(Guid.NewGuid());
        var machine = new Machine
        {
            Id = Guid.NewGuid(), Model = "Pump", Manufacture = "Acme", SerialNumber = "SN-123",
            Color = "Blue", InUseSince = Instant.FromUtc(2024, 1, 15, 12, 0),
            Location = new Location { Name = "Workshop" }
        };
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Machine>>(specification => specification is MachineForMaintenanceRecordSpecification),
                cancellationToken)
            .Returns(machine);
        var handler = new GetMachineByMaintenanceRecordIdQueryHandler(repository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new MachineInMaintenanceRecordDetailDto
        {
            Id = machine.Id,
            Model = machine.Model,
            Manufacture = machine.Manufacture,
            SerialNumber = machine.SerialNumber,
            Color = machine.Color,
            InUseSince = "15.01.2024",
            LocationName = machine.Location.Name
        };
        result.Should().BeEquivalentTo(expected);
        await repository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Machine>>(specification => specification is MachineForMaintenanceRecordSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsMaintenanceRecordNotFound_WhenRepositoryReturnsNoMachine()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetMachineByMaintenanceRecordIdQuery(Guid.NewGuid());
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Machine>>(specification => specification is MachineForMaintenanceRecordSpecification),
                cancellationToken)
            .Returns((Machine?)null);
        var handler = new GetMachineByMaintenanceRecordIdQueryHandler(repository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.MaintenanceRecordNotFound);
        await repository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Machine>>(specification => specification is MachineForMaintenanceRecordSpecification),
            cancellationToken);
    }
}
