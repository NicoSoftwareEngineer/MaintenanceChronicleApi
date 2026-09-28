using System.Net.Mail;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Utilities.Options;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MaintenanceChronicle.Application.EmailMessages;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions) : IEmailSender
{
    public async Task SendAsync(EmailMessage emailMessage, CancellationToken cancellationToken)
    {
        using var mail = new MailMessage
        {
            Subject = emailMessage.Subject,
            Body = emailMessage.Body,
            IsBodyHtml = true,
            From = new MailAddress(emailMessage.FromEmail, emailMessage.FromName)
        };

        foreach (var recipient in emailMessage.Recipients)
        {
            mail.To.Add(new MailAddress(recipient.Key, recipient.Value));
        }

        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await smtp.ConnectAsync(smtpOptions.Value.Host, smtpOptions.Value.Port,
            cancellationToken: cancellationToken);
        await smtp.AuthenticateAsync(smtpOptions.Value.Username, smtpOptions.Value.Password,
            cancellationToken);
        await smtp.SendAsync((MimeMessage)mail, cancellationToken);
    }
}
