using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands.Dto;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.Options;
using MediatR;
using Microsoft.Extensions.Options;
using NodaTime;

namespace MaintenanceChronicle.Application.EmailMessages.Commands;
/// <summary>
/// Handler for <see cref="CreateNewEmailMessageCommand"/>
/// </summary>
public class CreateNewEmailMessageCommandHandler(IRepository<EmailMessage> emailRepository, IUnitOfWork uow, IClock clock, IOptions<EnvironmentOptions> environmentOptions) : IRequestHandler<CreateNewEmailMessageCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewEmailMessageCommand request, CancellationToken cancellationToken)
    {
        request.NewEmailMessage.FromEmail ??= environmentOptions.Value.SenderEmail;
        request.NewEmailMessage.FromName ??= environmentOptions.Value.SenderName;

        var entity = request.NewEmailMessage.ToEntity(clock.GetCurrentInstant());

        await emailRepository.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
