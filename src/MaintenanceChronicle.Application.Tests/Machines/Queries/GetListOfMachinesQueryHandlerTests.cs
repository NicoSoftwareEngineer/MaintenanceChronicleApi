using System.Linq.Expressions;
using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Machines.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Machines.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Machines.Queries;

public class GetListOfMachinesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMachinesMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Machine> machines =
        [
            new Machine
            {
                Id = Guid.NewGuid(),
                Model = "Pump A",
                Location = new Location { Name = "Workshop", Customer = new Customer { Name = "Acme" } }
            },
            new Machine
            {
                Id = Guid.NewGuid(),
                Model = "Pump B",
                Location = new Location { Name = "Warehouse", Customer = new Customer { Name = "Contoso" } }
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1))
            .Returns(machines);
        var handler = new GetListOfMachinesQueryHandler(repository);

        // Act
        var result = await handler.Handle(new GetListOfEntityQuery<MachineInListDto>(), cancellationToken);

        // Assert
        var expected = machines.Select(machine => new MachineInListDto
        {
            Id = machine.Id,
            Model = machine.Model,
            LocationName = machine.Location.Name,
            CustomerName = machine.Location.Customer.Name
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await repository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenThereAreNoMachines()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.ListAsync(cancellationToken,
                Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1))
            .Returns((IReadOnlyList<Machine>)[]);
        var handler = new GetListOfMachinesQueryHandler(repository);

        // Act
        var result = await handler.Handle(new GetListOfEntityQuery<MachineInListDto>(), cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await repository.Received(1).ListAsync(cancellationToken,
            Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1));
    }

    [Fact]
    public async Task Handle_RequestsOneExtraMachineWithLocationAndCustomer_WhenPageIsSpecified()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<Machine> machines =
        [
            new Machine
            {
                Id = Guid.NewGuid(),
                Model = "Pump A",
                Location = new Location { Name = "Workshop", Customer = new Customer { Name = "Acme" } }
            }
        ];
        var repository = Substitute.For<IReadOnlyRepository<Machine>>();
        repository.ListPageAsync(5, 6, cancellationToken,
                Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1))
            .Returns(machines);
        var handler = new GetListOfMachinesQueryHandler(repository);
        var query = new GetListOfEntityQuery<MachineInListDto>(new PageRequest(2, 5));

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new MachineInListDto
        {
            Id = machines[0].Id,
            Model = machines[0].Model,
            LocationName = machines[0].Location.Name,
            CustomerName = machines[0].Location.Customer.Name
        });
        await repository.Received(1).ListPageAsync(5, 6, cancellationToken,
            Arg.Is<Expression<Func<Machine, object>>[]>(includes => includes.Length == 1));
        await repository.DidNotReceive().ListAsync(cancellationToken,
            Arg.Any<Expression<Func<Machine, object>>[]>());
    }
}
