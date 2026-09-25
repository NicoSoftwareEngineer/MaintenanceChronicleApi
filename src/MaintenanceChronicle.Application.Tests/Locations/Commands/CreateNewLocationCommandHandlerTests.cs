using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Application.Contracts.Locations.Commands.Dto;
using MaintenanceChronicle.Application.Locations.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Commands;

public class CreateNewLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsLocationAndSavesChanges_WhenCustomerExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var customer = new Customer { Id = Guid.NewGuid() };
        var tenantId = Guid.NewGuid();
        var generatedId = Guid.NewGuid();
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var command = new CreateNewLocationCommand(
            new NewLocationDto
            {
                CustomerId = customer.Id,
                Name = "Workshop",
                Street = "Main Street",
                City = "Prague",
                Country = "Czechia"
            },
            "creating-user",
            tenantId.ToString());

        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(customer.Id, cancellationToken).Returns(customer);

        var locationRepository = Substitute.For<IRepository<Location>>();
        Location? addedLocation = null;
        locationRepository.AddAsync(Arg.Any<Location>(), cancellationToken)
            .Returns(call =>
            {
                var location = call.Arg<Location>();
                location.Id = generatedId;
                addedLocation = location;
                return Task.FromResult(location);
            });

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new CreateNewLocationCommandHandler(customerRepository, locationRepository, uow, clock);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        result.Should().Be(generatedId);
        addedLocation.Should().NotBeNull();
        addedLocation!.CustomerId.Should().Be(customer.Id);
        addedLocation.Name.Should().Be(command.LocationDto.Name);
        addedLocation.Street.Should().Be(command.LocationDto.Street);
        addedLocation.City.Should().Be(command.LocationDto.City);
        addedLocation.Country.Should().Be(command.LocationDto.Country);
        addedLocation.TenantId.Should().Be(tenantId);
        addedLocation.CreatedBy.Should().Be(command.UserId);
        addedLocation.CreatedAt.Should().Be(now);
        addedLocation.ModifiedBy.Should().Be(command.UserId);
        addedLocation.ModifiedAt.Should().Be(now);
        await customerRepository.Received(1).GetByIdAsync(customer.Id, cancellationToken);
        await locationRepository.Received(1).AddAsync(Arg.Any<Location>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsCustomerNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewLocationCommand(
            new NewLocationDto
            {
                CustomerId = Guid.NewGuid(),
                Name = "Workshop",
                Street = "Main Street",
                City = "Prague",
                Country = "Czechia"
            },
            "creating-user",
            Guid.NewGuid().ToString());

        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(command.LocationDto.CustomerId, cancellationToken).Returns((Customer?)null);
        var locationRepository = Substitute.For<IRepository<Location>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new CreateNewLocationCommandHandler(customerRepository, locationRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.CustomerNotFound);
        await customerRepository.Received(1).GetByIdAsync(command.LocationDto.CustomerId, cancellationToken);
        await locationRepository.DidNotReceive().AddAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingLocationFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var customer = new Customer { Id = Guid.NewGuid() };
        var command = new CreateNewLocationCommand(
            new NewLocationDto
            {
                CustomerId = customer.Id,
                Name = "Workshop",
                Street = "Main Street",
                City = "Prague",
                Country = "Czechia"
            },
            "creating-user",
            Guid.NewGuid().ToString());

        var customerRepository = Substitute.For<IReadOnlyRepository<Customer>>();
        customerRepository.GetByIdAsync(customer.Id, cancellationToken).Returns(customer);
        var locationRepository = Substitute.For<IRepository<Location>>();
        var failure = new InvalidOperationException("Adding the location failed.");
        locationRepository.AddAsync(Arg.Any<Location>(), cancellationToken)
            .Returns(_ => Task.FromException<Location>(failure));
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewLocationCommandHandler(customerRepository, locationRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await locationRepository.Received(1).AddAsync(Arg.Any<Location>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
