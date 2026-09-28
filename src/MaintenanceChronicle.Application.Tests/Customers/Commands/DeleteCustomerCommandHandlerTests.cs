using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Utils.Commands;
using MaintenanceChronicle.Application.Customers.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Commands;

public class DeleteCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesCustomerAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new DeleteEntityByIdCommand<Customer>(Guid.NewGuid(), "deleting-user");
        var customer = new Customer { Id = command.Id };

        var customerRepository = Substitute.For<IRepository<Customer>>();
        customerRepository.GetByIdAsync(command.Id, cancellationToken).Returns(customer);

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);

        var handler = new DeleteCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        customer.DeletedBy.Should().Be(command.UserId);
        customer.DeletedAt.Should().Be(now);
        await customerRepository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsCustomerNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new DeleteEntityByIdCommand<Customer>(Guid.NewGuid(), "deleting-user");

        var customerRepository = Substitute.For<IRepository<Customer>>();
        customerRepository.GetByIdAsync(command.Id, cancellationToken).Returns((Customer?)null);

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new DeleteCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.CustomerNotFound);
        await customerRepository.Received(1).GetByIdAsync(command.Id, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
