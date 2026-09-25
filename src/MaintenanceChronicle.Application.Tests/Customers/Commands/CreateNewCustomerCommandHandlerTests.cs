using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Customers.Commands;
using MaintenanceChronicle.Application.Contracts.Customers.Commands.Dto;
using MaintenanceChronicle.Application.Customers.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Commands;

public class CreateNewCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsCustomerAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var tenantId = Guid.NewGuid();
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new CreateNewCustomerCommand(
            new NewCustomerDto
            {
                Name = "New Customer",
                Email = "customer@example.com",
                PhoneNumber = "111222333",
                CompanyIdNumber = "10001"
            },
            "creating-user",
            tenantId.ToString());

        var customerRepository = Substitute.For<IRepository<Customer>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);

        Customer? addedCustomer = null;
        customerRepository.AddAsync(Arg.Any<Customer>(), cancellationToken)
            .Returns(call =>
            {
                var customer = call.Arg<Customer>();
                addedCustomer = customer;
                return Task.FromResult(customer);
            });

        var handler = new CreateNewCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeEmpty();
        addedCustomer.Should().NotBeNull();
        addedCustomer!.Id.Should().Be(result);
        addedCustomer.Name.Should().Be(command.NewCustomer.Name);
        addedCustomer.Email.Should().Be(command.NewCustomer.Email);
        addedCustomer.PhoneNumber.Should().Be(command.NewCustomer.PhoneNumber);
        addedCustomer.CompanyIdNumber.Should().Be(command.NewCustomer.CompanyIdNumber);
        addedCustomer.TenantId.Should().Be(tenantId);
        addedCustomer.CreatedBy.Should().Be(command.UserId);
        addedCustomer.CreatedAt.Should().Be(now);
        addedCustomer.ModifiedBy.Should().Be(command.UserId);
        addedCustomer.ModifiedAt.Should().Be(now);

        await customerRepository.Received(1).AddAsync(Arg.Any<Customer>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingCustomerFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewCustomerCommand(
            new NewCustomerDto
            {
                Name = "New Customer",
                Email = "customer@example.com",
                PhoneNumber = "111222333",
                CompanyIdNumber = "10001"
            },
            "creating-user",
            Guid.NewGuid().ToString());

        var customerRepository = Substitute.For<IRepository<Customer>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));

        var failure = new InvalidOperationException("Adding the customer failed.");
        customerRepository.AddAsync(Arg.Any<Customer>(), cancellationToken)
            .Returns(_ => Task.FromException<Customer>(failure));

        var handler = new CreateNewCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await customerRepository.Received(1).AddAsync(Arg.Any<Customer>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
