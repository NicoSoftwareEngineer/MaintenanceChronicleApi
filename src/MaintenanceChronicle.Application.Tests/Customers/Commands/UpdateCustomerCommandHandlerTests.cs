using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Customers.Commands;
using MaintenanceChronicle.Application.Contracts.Customers.Commands.Dto;
using MaintenanceChronicle.Application.Customers.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using Microsoft.AspNetCore.JsonPatch;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Customers.Commands;

public class UpdateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesPatchToCustomerAndSavesChanges()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var createdAt = Instant.FromUtc(2026, 1, 1, 12, 0);
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Original Customer",
            Email = "original@example.com",
            PhoneNumber = "111222333",
            CompanyIdNumber = "10001",
            CreatedBy = "creating-user",
            CreatedAt = createdAt
        };

        var patch = new JsonPatchDocument<ManageCustomerDetailDto>();
        patch.Replace(dto => dto.Name, "Updated Customer");
        patch.Replace(dto => dto.Email, "updated@example.com");
        var command = new UpdateCustomerCommand(patch, customer.Id, "updating-user");

        var customerRepository = Substitute.For<IRepository<Customer>>();
        customerRepository.GetByIdAsync(command.CustomerId, cancellationToken).Returns(customer);

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);

        var handler = new UpdateCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        var expected = new ManageCustomerDetailDto
        {
            Id = customer.Id,
            Name = "Updated Customer",
            Email = "updated@example.com",
            PhoneNumber = "111222333",
            CompanyIdNumber = "10001"
        };

        result.Should().BeEquivalentTo(expected);
        customer.Name.Should().Be(expected.Name);
        customer.Email.Should().Be(expected.Email);
        customer.PhoneNumber.Should().Be(expected.PhoneNumber);
        customer.CompanyIdNumber.Should().Be(expected.CompanyIdNumber);
        customer.CreatedBy.Should().Be("creating-user");
        customer.CreatedAt.Should().Be(createdAt);
        customer.ModifiedBy.Should().Be(command.UserId);
        customer.ModifiedAt.Should().Be(now);

        await customerRepository.Received(1).GetByIdAsync(command.CustomerId, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsCustomerNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var patch = new JsonPatchDocument<ManageCustomerDetailDto>();
        patch.Replace(dto => dto.Name, "Updated Customer");
        var command = new UpdateCustomerCommand(patch, Guid.NewGuid(), "updating-user");

        var customerRepository = Substitute.For<IRepository<Customer>>();
        customerRepository.GetByIdAsync(command.CustomerId, cancellationToken).Returns((Customer?)null);

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new UpdateCustomerCommandHandler(customerRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.CustomerNotFound);
        await customerRepository.Received(1).GetByIdAsync(command.CustomerId, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }
}
