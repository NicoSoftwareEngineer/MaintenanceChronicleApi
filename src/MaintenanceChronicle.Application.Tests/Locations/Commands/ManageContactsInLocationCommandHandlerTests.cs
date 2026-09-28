using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Locations.Commands;
using MaintenanceChronicle.Application.Locations.Commands;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Commands;

public class ManageContactsInLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsNewContactAndSoftDeletesRemovedContact()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var tenantId = Guid.NewGuid();
        var retainedUserId = Guid.NewGuid();
        var removedUserId = Guid.NewGuid();
        var newUser = new User { Id = Guid.NewGuid(), FirstName = "New", LastName = "Contact" };
        var retainedContact = new LocationContactUser { UserId = retainedUserId };
        var removedContact = new LocationContactUser { UserId = removedUserId };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Contacts = [retainedContact, removedContact]
        };
        var command = new ManageContactsInLocationCommand(
            location.Id,
            [
                new LocationContactInListDto
                {
                    Id = retainedUserId,
                    Name = "Retained Contact",
                    Email = "retained@example.com"
                },
                new LocationContactInListDto
                {
                    Id = newUser.Id,
                    Name = "New Contact",
                    Email = "new@example.com"
                }
            ],
            "updating-user",
            tenantId.ToString());

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Location>>(specification => specification is LocationWithContactsSpecification),
                cancellationToken)
            .Returns(location);
        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.GetByIdAsync(newUser.Id, cancellationToken).Returns(newUser);

        var contactRepository = Substitute.For<IRepository<LocationContactUser>>();
        LocationContactUser? addedContact = null;
        contactRepository.AddAsync(Arg.Any<LocationContactUser>(), cancellationToken)
            .Returns(call =>
            {
                var contact = call.Arg<LocationContactUser>();
                addedContact = contact;
                return Task.FromResult(contact);
            });

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new ManageContactsInLocationCommandHandler(
            locationRepository, userRepository, contactRepository, uow, clock);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        retainedContact.DeletedAt.Should().BeNull();
        retainedContact.DeletedBy.Should().BeNull();
        removedContact.DeletedAt.Should().Be(now);
        removedContact.DeletedBy.Should().Be(command.UserId);
        addedContact.Should().NotBeNull();
        addedContact!.UserId.Should().Be(newUser.Id);
        addedContact.LocationId.Should().Be(location.Id);
        addedContact.TenantId.Should().Be(tenantId);
        addedContact.CreatedAt.Should().Be(now);
        addedContact.CreatedBy.Should().Be(command.UserId);
        addedContact.ModifiedAt.Should().Be(now);
        addedContact.ModifiedBy.Should().Be(command.UserId);
        await locationRepository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Location>>(specification => specification is LocationWithContactsSpecification),
            cancellationToken);
        await userRepository.Received(1).GetByIdAsync(newUser.Id, cancellationToken);
        await contactRepository.Received(1).AddAsync(Arg.Any<LocationContactUser>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsLocationNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new ManageContactsInLocationCommand(
            Guid.NewGuid(), [], "updating-user", Guid.NewGuid().ToString());

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Location>>(specification => specification is LocationWithContactsSpecification),
                cancellationToken)
            .Returns((Location?)null);
        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        var contactRepository = Substitute.For<IRepository<LocationContactUser>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var handler = new ManageContactsInLocationCommandHandler(
            locationRepository, userRepository, contactRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.LocationNotFound);
        await locationRepository.Received(1).GetBySpecificationAsync(
            Arg.Is<ISpecification<Location>>(specification => specification is LocationWithContactsSpecification),
            cancellationToken);
        await userRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await contactRepository.DidNotReceive().AddAsync(
            Arg.Any<LocationContactUser>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        clock.DidNotReceive().GetCurrentInstant();
    }

    [Fact]
    public async Task Handle_ThrowsUserNotFound_WhenNewContactUserDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var userId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid() };
        var command = new ManageContactsInLocationCommand(
            location.Id,
            [new LocationContactInListDto
            {
                Id = userId,
                Name = "Missing Contact",
                Email = "missing@example.com"
            }],
            "updating-user",
            Guid.NewGuid().ToString());

        var locationRepository = Substitute.For<IRepository<Location>>();
        locationRepository.GetBySpecificationAsync(
                Arg.Is<ISpecification<Location>>(specification => specification is LocationWithContactsSpecification),
                cancellationToken)
            .Returns(location);
        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.GetByIdAsync(userId, cancellationToken).Returns((User?)null);
        var contactRepository = Substitute.For<IRepository<LocationContactUser>>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new ManageContactsInLocationCommandHandler(
            locationRepository, userRepository, contactRepository, uow, clock);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.UserNotFound);
        await userRepository.Received(1).GetByIdAsync(userId, cancellationToken);
        await contactRepository.DidNotReceive().AddAsync(
            Arg.Any<LocationContactUser>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
