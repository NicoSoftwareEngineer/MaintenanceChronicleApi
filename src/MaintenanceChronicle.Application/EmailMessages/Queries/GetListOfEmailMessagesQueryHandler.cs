using MaintenanceChronicle.Application.Contracts.EmailMessages.Queries.Dto;
using MaintenanceChronicle.Application.Contracts.Utils.Queries;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.Persistence;
using MediatR;

namespace MaintenanceChronicle.Application.EmailMessages.Queries;
/// <summary>
/// Handler for <see cref="GetListOfEntityQuery{EmailMessageInListDto}"/>
/// </summary>
public class GetListOfEmailMessagesQueryHandler(IReadOnlyRepository<EmailMessage> emailReadOnlyRepository) : IRequestHandler<GetListOfEntityQuery<EmailMessageInListDto>, List<EmailMessageInListDto>>
{
    public async Task<List<EmailMessageInListDto>> Handle(GetListOfEntityQuery<EmailMessageInListDto> request,
        CancellationToken cancellationToken)
    {
        var emailEntities = await emailReadOnlyRepository.ListAsync(cancellationToken);
        var list = emailEntities.Select(email => email.ToListDto()).ToList();

        return list;
    }
}
