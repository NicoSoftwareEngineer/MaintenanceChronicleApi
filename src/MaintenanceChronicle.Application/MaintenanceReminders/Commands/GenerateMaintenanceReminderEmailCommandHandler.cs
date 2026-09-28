using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Specifications;
using MaintenanceChronicle.Infrastructure.Persistence;
using MaintenanceChronicle.Utilities.EmailTemplates;
using MaintenanceChronicle.Utilities.Error;
using MediatR;
using NodaTime.Text;

namespace MaintenanceChronicle.Application.MaintenanceReminders.Commands;
/// <summary>
/// Handler for <see cref="GenerateMaintenanceReminderEmailCommand"/>.
/// </summary>
public class GenerateMaintenanceReminderEmailCommandHandler(IReadOnlyRepository<Machine> machineReadOnlyRepository) : IRequestHandler<GenerateMaintenanceReminderEmailCommand, NewEmailMessageDto>
{
    public async Task<NewEmailMessageDto> Handle(GenerateMaintenanceReminderEmailCommand request,
        CancellationToken cancellationToken)
    {
        // Get machine for reminder
        var specification = new MachineWithLocationContactsSpecification(request.Reminder.MachineId);
        var machine = await machineReadOnlyRepository.GetBySpecificationAsync(specification, cancellationToken);
        if (machine == null)
        {
            throw new BadRequestException(ErrorType.MachineNotFound);
        }

        // Get date in format yyyy-MM-dd
        var date = InstantPattern.CreateWithInvariantCulture("yyyy-MM-dd").Format(request.Reminder.Date);

        // Get email body
        var emailHelper = new EmailTemplateHelper();
        var body = await emailHelper.GetMaintenanceReminderEmailTemplate($"{machine.Manufacture} {machine.Model}", machine.SerialNumber, date, request.Reminder.Description, machine.Location.Name, $"{machine.Location.Street}, {machine.Location.City}, {machine.Location.Country}");

        // Get users for email
        var users = machine.Location.Contacts.Select(c => c.User).ToList();
        // Create email message
        var emailMessage = new NewEmailMessageDto
        {
            Body = body,
            Subject = "Maintenance Reminder",
            Recipients = users.Select(u =>(u.Email!, (string?)$"{u.FirstName} {u.LastName}")).ToDictionary()
        };

        return emailMessage;
    }
}
