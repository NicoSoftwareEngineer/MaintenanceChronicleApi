using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.LocationContactUsers.Queries;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.LocationContactUsers.Queries;

public class GetListOfLocationsForContactUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLocationsMappedToListDtos_WhenUserExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = new User { Id = Guid.NewGuid(), FirstName = "Contact", LastName = "User" };
        var query = new GetListOfLocationsForContactUserQuery(user.Id);
        IReadOnlyList<LocationContactUser> contacts =
        [
            new LocationContactUser
            {
                UserId = user.Id,
                Location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = "First Location",
                    Street = "First Street",
                    City = "First City"
                }
            },
            new LocationContactUser
            {
                UserId = user.Id,
                Location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = "Second Location",
                    Street = "Second Street",
                    City = "Second City"
                }
            }
        ];

        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.GetByIdAsync(query.UserId, cancellationToken).Returns(user);

        var contactRepository = Substitute.For<IReadOnlyRepository<LocationContactUser>>();
        contactRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                    specification is LocationContactsForUserSpecification),
                cancellationToken)
            .Returns(contacts);

        var handler = new GetListOfLocationsForContactUserQueryHandler(userRepository, contactRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new[]
        {
            new LocationInListForContactDto
            {
                Name = contacts[0].Location.Name,
                Address = $"{contacts[0].Location.Street}, {contacts[0].Location.City}"
            },
            new LocationInListForContactDto
            {
                Name = contacts[1].Location.Name,
                Address = $"{contacts[1].Location.Street}, {contacts[1].Location.City}"
            }
        };

        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await userRepository.Received(1).GetByIdAsync(query.UserId, cancellationToken);
        await contactRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                specification is LocationContactsForUserSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenUserHasNoLocations()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var user = new User { Id = Guid.NewGuid(), FirstName = "Contact", LastName = "User" };
        var query = new GetListOfLocationsForContactUserQuery(user.Id);
        IReadOnlyList<LocationContactUser> contacts = [];

        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.GetByIdAsync(query.UserId, cancellationToken).Returns(user);

        var contactRepository = Substitute.For<IReadOnlyRepository<LocationContactUser>>();
        contactRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                    specification is LocationContactsForUserSpecification),
                cancellationToken)
            .Returns(contacts);

        var handler = new GetListOfLocationsForContactUserQueryHandler(userRepository, contactRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await userRepository.Received(1).GetByIdAsync(query.UserId, cancellationToken);
        await contactRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                specification is LocationContactsForUserSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetListOfLocationsForContactUserQuery(Guid.NewGuid());

        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.GetByIdAsync(query.UserId, cancellationToken).Returns((User?)null);

        var contactRepository = Substitute.For<IReadOnlyRepository<LocationContactUser>>();
        var handler = new GetListOfLocationsForContactUserQueryHandler(userRepository, contactRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.UserNotFound);
        await userRepository.Received(1).GetByIdAsync(query.UserId, cancellationToken);
        await contactRepository.DidNotReceive().ListBySpecificationAsync(
            Arg.Any<IListSpecification<LocationContactUser>>(), Arg.Any<CancellationToken>());
    }
}
