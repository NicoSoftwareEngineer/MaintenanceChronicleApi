using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Application.EmailMessages.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.EmailMessages.Queries;

public class GetListOfEmailMessagesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmailMessagesMappedToListDtos()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<EmailMessage> messages =
        [
            new EmailMessage
            {
                Id = Guid.NewGuid(),
                Recipients = new Dictionary<string, string?> { ["first@example.com"] = "First" },
                Subject = "First subject",
                Body = "First body",
                Sent = false,
                FromEmail = "sender@example.com",
                FromName = "Sender"
            },
            new EmailMessage
            {
                Id = Guid.NewGuid(),
                Recipients = new Dictionary<string, string?> { ["second@example.com"] = "Second" },
                Subject = "Second subject",
                Body = "Second body",
                Sent = true,
                FromEmail = "other@example.com",
                FromName = "Other Sender"
            }
        ];

        var emailRepository = Substitute.For<IReadOnlyRepository<EmailMessage>>();
        emailRepository.ListAsync(cancellationToken).Returns(messages);

        var handler = new GetListOfEmailMessagesQueryHandler(emailRepository);
        var query = new GetListOfEntityQuery<EmailMessageInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        var expected = messages.Select(message => new EmailMessageInListDto
        {
            Id = message.Id,
            Recipients = message.Recipients,
            Subject = message.Subject,
            Body = message.Body,
            Sent = message.Sent,
            FromEmail = message.FromEmail,
            FromName = message.FromName
        });

        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        await emailRepository.Received(1).ListAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenRepositoryReturnsNoMessages()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        IReadOnlyList<EmailMessage> messages = [];

        var emailRepository = Substitute.For<IReadOnlyRepository<EmailMessage>>();
        emailRepository.ListAsync(cancellationToken).Returns(messages);

        var handler = new GetListOfEmailMessagesQueryHandler(emailRepository);
        var query = new GetListOfEntityQuery<EmailMessageInListDto>();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        await emailRepository.Received(1).ListAsync(cancellationToken);
    }
}
