using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Queries;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Machines.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Queries;

public class GetMachinesForLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMachinesForLocation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var location = new Location { Id = Guid.NewGuid() };
        var query = new GetMachinesForLocationQuery(location.Id);
        IReadOnlyList<Machine> machines =
        [
            new Machine { Id = Guid.NewGuid(), LocationId = location.Id, Model = "Pump", SerialNumber = "SN-1" },
            new Machine { Id = Guid.NewGuid(), LocationId = location.Id, Model = "Compressor", SerialNumber = "SN-2" }
        ];
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(location.Id, cancellationToken).Returns(location);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<Machine>>(specification => specification is MachinesForLocationSpecification),
                cancellationToken)
            .Returns(machines);
        var handler = new GetMachinesForLocationQueryHandler(locationRepository, machineRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = machines.Select(machine => new MachineInListForLocationDto
        {
            Id = machine.Id,
            Model = machine.Model,
            SerialNumber = machine.SerialNumber
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await locationRepository.Received(1).GetByIdAsync(location.Id, cancellationToken);
        await machineRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<Machine>>(specification => specification is MachinesForLocationSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenLocationHasNoMachines()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var location = new Location { Id = Guid.NewGuid() };
        var query = new GetMachinesForLocationQuery(location.Id);
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(location.Id, cancellationToken).Returns(location);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        machineRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<Machine>>(specification => specification is MachinesForLocationSpecification),
                cancellationToken)
            .Returns((IReadOnlyList<Machine>)[]);
        var handler = new GetMachinesForLocationQueryHandler(locationRepository, machineRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await machineRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<Machine>>(specification => specification is MachinesForLocationSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetMachinesForLocationQuery(Guid.NewGuid());
        var locationRepository = Substitute.For<IReadOnlyRepository<Location>>();
        locationRepository.GetByIdAsync(query.LocationId, cancellationToken).Returns((Location?)null);
        var machineRepository = Substitute.For<IReadOnlyRepository<Machine>>();
        var handler = new GetMachinesForLocationQueryHandler(locationRepository, machineRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetByIdAsync(query.LocationId, cancellationToken);
        await machineRepository.DidNotReceive().ListBySpecificationAsync(
            Arg.Any<IListSpecification<Machine>>(), Arg.Any<CancellationToken>());
    }
}
