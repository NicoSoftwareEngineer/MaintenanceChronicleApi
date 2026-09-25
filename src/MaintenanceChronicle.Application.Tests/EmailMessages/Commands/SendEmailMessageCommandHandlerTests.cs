using FluentAssertions;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.EmailMessages;
using MaintenanceChronicle.Application.EmailMessages.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using NSubstitute;

namespace MaintenanceChronicle.Application.Tests.EmailMessages.Commands;

public class SendEmailMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsEmailMessageAndMarksItAsSent()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new SendEmailMessageCommand(Guid.NewGuid());
        var message = new EmailMessage
        {
            Id = command.MessageId,
            Subject = "Subject",
            Body = "Body",
            FromEmail = "sender@example.com",
            FromName = "Sender",
            Recipients = new Dictionary<string, string?> { ["recipient@example.com"] = "Recipient" }
        };

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        emailRepository.GetByIdAsync(command.MessageId, cancellationToken).Returns(message);

        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(message, cancellationToken).Returns(Task.CompletedTask);
        var handler = new SendEmailMessageCommandHandler(emailRepository, uow, emailSender);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        message.Sent.Should().BeTrue();
        await emailRepository.Received(1).GetByIdAsync(command.MessageId, cancellationToken);
        await emailSender.Received(1).SendAsync(message, cancellationToken);
        await uow.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ThrowsEmailMessageNotFound_WhenMessageDoesNotExist()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new SendEmailMessageCommand(Guid.NewGuid());

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        emailRepository.GetByIdAsync(command.MessageId, cancellationToken).Returns((EmailMessage?)null);

        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new SendEmailMessageCommandHandler(emailRepository, uow, emailSender);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.EmailMessageNotFound);
        await emailRepository.Received(1).GetByIdAsync(command.MessageId, cancellationToken);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsEmailAlreadySent_WhenMessageWasSentEarlier()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new SendEmailMessageCommand(Guid.NewGuid());
        var message = new EmailMessage { Id = command.MessageId, Sent = true };

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        emailRepository.GetByIdAsync(command.MessageId, cancellationToken).Returns(message);

        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new SendEmailMessageCommandHandler(emailRepository, uow, emailSender);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorType.Should().Be(ErrorType.EmailAlreadySent);
        message.Sent.Should().BeTrue();
        await emailRepository.Received(1).GetByIdAsync(command.MessageId, cancellationToken);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotMarkMessageAsSent_WhenSendingFails()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var command = new SendEmailMessageCommand(Guid.NewGuid());
        var message = new EmailMessage { Id = command.MessageId, Sent = false };

        var emailRepository = Substitute.For<IRepository<EmailMessage>>();
        emailRepository.GetByIdAsync(command.MessageId, cancellationToken).Returns(message);

        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();
        var failure = new InvalidOperationException("Sending the email message failed.");
        emailSender.SendAsync(message, cancellationToken).Returns(Task.FromException(failure));
        var handler = new SendEmailMessageCommandHandler(emailRepository, uow, emailSender);

        // Act
        Func<Task> act = async () => await handler.Handle(command, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        message.Sent.Should().BeFalse();
        await emailRepository.Received(1).GetByIdAsync(command.MessageId, cancellationToken);
        await emailSender.Received(1).SendAsync(message, cancellationToken);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
