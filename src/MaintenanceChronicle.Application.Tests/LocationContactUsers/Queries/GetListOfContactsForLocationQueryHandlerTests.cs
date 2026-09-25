using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries;
using MaintenanceChronicle.Application.Contracts.LocationContactUsers.Queries.Dto;
using MaintenanceChronicle.Application.LocationContactUsers.Queries;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.LocationContactUsers.Queries;

public class GetListOfContactsForLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsContactsMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetListOfContactsForLocationQuery(Guid.NewGuid());
        var firstUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "First",
            LastName = "Contact",
            Email = "first@example.com",
            PhoneNumber = "111222333"
        };
        var secondUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Second",
            LastName = "Contact",
            Email = "second@example.com"
        };
        IReadOnlyList<LocationContactUser> contacts =
        [
            new LocationContactUser
            {
                Id = Guid.NewGuid(),
                LocationId = query.LocationId,
                UserId = firstUser.Id,
                User = firstUser
            },
            new LocationContactUser
            {
                Id = Guid.NewGuid(),
                LocationId = query.LocationId,
                UserId = secondUser.Id,
                User = secondUser
            }
        ];

        var contactRepository = Substitute.For<IReadOnlyRepository<LocationContactUser>>();
        contactRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                    specification is LocationContactsForLocationSpecification),
                cancellationToken)
            .Returns(contacts);

        var handler = new GetListOfContactsForLocationQueryHandler(contactRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = new[]
        {
            new LocationContactInListDto
            {
                Id = firstUser.Id,
                Name = $"{firstUser.FirstName} {firstUser.LastName}",
                Email = firstUser.Email!,
                PhoneNumber = firstUser.PhoneNumber
            },
            new LocationContactInListDto
            {
                Id = secondUser.Id,
                Name = $"{secondUser.FirstName} {secondUser.LastName}",
                Email = secondUser.Email!,
                PhoneNumber = secondUser.PhoneNumber
            }
        };

        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await contactRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                specification is LocationContactsForLocationSpecification),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenThereAreNoContacts()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetListOfContactsForLocationQuery(Guid.NewGuid());
        IReadOnlyList<LocationContactUser> contacts = [];

        var contactRepository = Substitute.For<IReadOnlyRepository<LocationContactUser>>();
        contactRepository.ListBySpecificationAsync(
                Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                    specification is LocationContactsForLocationSpecification),
                cancellationToken)
            .Returns(contacts);

        var handler = new GetListOfContactsForLocationQueryHandler(contactRepository);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await contactRepository.Received(1).ListBySpecificationAsync(
            Arg.Is<IListSpecification<LocationContactUser>>(specification =>
                specification is LocationContactsForLocationSpecification),
            cancellationToken);
    }
}
