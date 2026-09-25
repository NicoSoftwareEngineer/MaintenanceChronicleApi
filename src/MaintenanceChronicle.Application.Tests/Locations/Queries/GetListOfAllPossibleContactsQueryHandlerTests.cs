using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.Locations.Queries;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.Locations.Queries;

public class GetListOfAllPossibleContactsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersMappedToContactDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<User> users =
        [
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "First",
                LastName = "Contact",
                Email = "first@example.com",
                PhoneNumber = "111222333"
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Second",
                LastName = "Contact",
                Email = "second@example.com"
            }
        ];

        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.ListAsync(cancellationToken).Returns(users);
        var handler = new GetListOfAllPossibleContactsQueryHandler(userRepository);
        var query = new GetListOfEntityQuery<LocationContactInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = users.Select(user => new LocationContactInListDto
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber
        });
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await userRepository.Received(1).ListAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenThereAreNoUsers()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<User> users = [];

        var userRepository = Substitute.For<IReadOnlyRepository<User>>();
        userRepository.ListAsync(cancellationToken).Returns(users);
        var handler = new GetListOfAllPossibleContactsQueryHandler(userRepository);
        var query = new GetListOfEntityQuery<LocationContactInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await userRepository.Received(1).ListAsync(cancellationToken);
    }
}
