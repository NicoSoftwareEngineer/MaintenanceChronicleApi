using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using MaintenanceChronicle.Utilities.Options;

namespace MaintenanceChronicle.Application.EmailMessages.Commands;
/// <summary>
/// Handler for <see cref="SendEmailMessageCommand"/>
/// </summary>
public class SendEmailMessageCommandHandler(IRepository<EmailMessage> emailRepository, IUnitOfWork uow, IOptions<SmtpOptions> smtpOptions) : IRequestHandler<SendEmailMessageCommand>
{
    public async Task Handle(SendEmailMessageCommand request, CancellationToken cancellationToken)
    {
        // Find the email message
        var emailMessage = await emailRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (emailMessage == null)
        {
            throw new BadRequestException(ErrorType.EmailMessageNotFound);
        }

        // Check if the email has already been sent
        if (emailMessage.Sent)
        {
            throw new BadRequestException(ErrorType.EmailAlreadySent);
        }

        // Create the email message
        using var mail = new MailMessage
        {
            Subject = emailMessage.Subject,
            Body = emailMessage.Body,
            IsBodyHtml = true,
            From = new MailAddress(emailMessage.FromEmail, emailMessage.FromName),
        };
        // Add the recipients
        foreach (var recipients in emailMessage.Recipients)
        {
            mail.To.Add(new MailAddress(recipients.Key, recipients.Value));

        }

        // Send the email
        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await smtp.ConnectAsync(smtpOptions.Value.Host, smtpOptions.Value.Port, cancellationToken: cancellationToken);
        await smtp.AuthenticateAsync(smtpOptions.Value.Username, smtpOptions.Value.Password, cancellationToken);
        await smtp.SendAsync((MimeMessage)mail, cancellationToken);

        emailMessage.Sent = true;

        await uow.SaveChangesAsync(cancellationToken);
    }
}
