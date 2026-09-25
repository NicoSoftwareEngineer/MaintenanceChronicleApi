using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.EmailMessages;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Error;
using MediatR;

namespace MaintenanceChronicle.Application.EmailMessages.Commands;
/// <summary>
/// Handler for <see cref="SendEmailMessageCommand"/>
/// </summary>
public class SendEmailMessageCommandHandler(IRepository<EmailMessage> emailRepository, IUnitOfWork uow, IEmailSender emailSender) : IRequestHandler<SendEmailMessageCommand>
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

        await emailSender.SendAsync(emailMessage, cancellationToken);

        emailMessage.Sent = true;

        await uow.SaveChangesAsync(cancellationToken);
    }
}
