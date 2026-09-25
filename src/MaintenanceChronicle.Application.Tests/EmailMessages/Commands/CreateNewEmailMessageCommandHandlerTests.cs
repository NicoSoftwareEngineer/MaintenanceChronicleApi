using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands.Dto;
using MaintenanceChronicle.Application.EmailMessages.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Options;
using Microsoft.Extensions.Options;
using NodaTime;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.EmailMessages.Commands;

public class CreateNewEmailMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_UsesConfiguredSenderAndAddsEmailMessage()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var now = Instant.FromUtc(2026, 9, 25, 12, 0);
        var generatedId = Guid.NewGuid();
        var command = new CreateNewEmailMessageCommand(new NewEmailMessageDto
        {
            Recipients = new Dictionary<string, string?> { ["recipient@example.com"] = "Recipient" },
            Subject = "Subject",
            Body = "Body"
        });
        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            FrontendHostUrl = "https://example.com",
            FrontendConfirmUrl = "https://example.com/confirm",
            FrontendPasswordResetUrl = "https://example.com/reset",
            FrontendPasswordCreateUrl = "https://example.com/create-password",
            SenderEmail = "default@example.com",
            SenderName = "Default Sender"
        });

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        EmailMessage? addedMessage = null;
        emailRepository.AddAsync(Arg.Any<EmailMessage>(), cancellationToken)
            .Returns(call =>
            {
                var message = call.Arg<EmailMessage>();
                message.Id = generatedId;
                addedMessage = message;
                return Task.FromResult(message);
            });

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        var handler = new CreateNewEmailMessageCommandHandler(emailRepository, uow, clock, environmentOptions);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        result.Should().Be(generatedId);
        addedMessage.Should().NotBeNull();
        addedMessage!.Recipients.Should().BeEquivalentTo(command.NewEmailMessage.Recipients);
        addedMessage.Subject.Should().Be(command.NewEmailMessage.Subject);
        addedMessage.Body.Should().Be(command.NewEmailMessage.Body);
        addedMessage.FromEmail.Should().Be(environmentOptions.Value.SenderEmail);
        addedMessage.FromName.Should().Be(environmentOptions.Value.SenderName);
        addedMessage.Sent.Should().BeFalse();
        addedMessage.CreatedAt.Should().Be(now);
        await emailRepository.Received(1).AddAsync(Arg.Any<EmailMessage>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_PreservesExplicitSenderValues()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewEmailMessageCommand(new NewEmailMessageDto
        {
            Subject = "Subject",
            Body = "Body",
            FromEmail = "custom@example.com",
            FromName = "Custom Sender"
        });
        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            FrontendHostUrl = "https://example.com",
            FrontendConfirmUrl = "https://example.com/confirm",
            FrontendPasswordResetUrl = "https://example.com/reset",
            FrontendPasswordCreateUrl = "https://example.com/create-password",
            SenderEmail = "default@example.com",
            SenderName = "Default Sender"
        });

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        EmailMessage? addedMessage = null;
        emailRepository.AddAsync(Arg.Any<EmailMessage>(), cancellationToken)
            .Returns(call =>
            {
                var message = call.Arg<EmailMessage>();
                message.Id = Guid.NewGuid();
                addedMessage = message;
                return Task.FromResult(message);
            });

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewEmailMessageCommandHandler(emailRepository, uow, clock, environmentOptions);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        addedMessage.Should().NotBeNull();
        result.Should().Be(addedMessage!.Id);
        addedMessage.FromEmail.Should().Be(command.NewEmailMessage.FromEmail);
        addedMessage.FromName.Should().Be(command.NewEmailMessage.FromName);
        await emailRepository.Received(1).AddAsync(Arg.Any<EmailMessage>(), cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_DoesNotSaveChanges_WhenAddingEmailMessageFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new CreateNewEmailMessageCommand(new NewEmailMessageDto
        {
            Subject = "Subject",
            Body = "Body",
            FromEmail = "sender@example.com",
            FromName = "Sender"
        });
        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            FrontendHostUrl = "https://example.com",
            FrontendConfirmUrl = "https://example.com/confirm",
            FrontendPasswordResetUrl = "https://example.com/reset",
            FrontendPasswordCreateUrl = "https://example.com/create-password",
            SenderEmail = "default@example.com",
            SenderName = "Default Sender"
        });

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        var failure = new InvalidOperationException("Adding the email message failed.");
        emailRepository.AddAsync(Arg.Any<EmailMessage>(), cancellationToken)
            .Returns(_ => Task.FromException<EmailMessage>(failure));

        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 9, 25, 12, 0));
        var handler = new CreateNewEmailMessageCommandHandler(emailRepository, uow, clock, environmentOptions);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        await emailRepository.Received(1).AddAsync(Arg.Any<EmailMessage>(), cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
