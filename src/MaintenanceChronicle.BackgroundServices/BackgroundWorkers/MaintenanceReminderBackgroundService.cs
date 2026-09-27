using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands;
using MaintenanceChronicle.Application.Contracts.EmailMessages.Commands.Dto;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Commands;
using MaintenanceChronicle.Application.Contracts.MaintenanceReminders.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaintenanceChronicle.BackgroundServices.BackgroundWorkers;

public class MaintenanceReminderBackgroundService(ISystemTenantScopeFactory scopeFactory) : BackgroundService
{
    /// <summary>
    /// Function definition from BackgroundService, which gets called at the start of an app
    /// Calls private CheckReminders
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await CheckReminders(cancellationToken);
    }
    /// <summary>
    /// Has infinite loop, gets all due reminders, generates email for each, and marks reminder as sent
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task CheckReminders(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            //Gets mediator
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            //Gets all due reminders
            var query = new GetAllDueMaintenanceRemindersQuery();
            var dueReminders = await mediator.Send(query, cancellationToken);

            //init list of emails to be created, where Item1 is reminder id and Item2 is email
            var emailsToBeCreated = new List<(Guid, NewEmailMessageDto)>();

            //For each due reminder that was not sent, generate email and assign it to emailsToBeCreated
            foreach (var reminder in dueReminders.Where(mr => !mr.WasSent))
            {
                var generateEmailCommand = new GenerateMaintenanceReminderEmailCommand(reminder);
                var email= await mediator.Send(generateEmailCommand, cancellationToken);
                emailsToBeCreated.Add((reminder.Id,email));
            }

            //For each email in emailsToBeCreated, create email and mark reminder as sent
            foreach (var email in emailsToBeCreated)
            {
                var createEmailCommand = new CreateNewEmailMessageCommand(email.Item2);
                var result =await mediator.Send(createEmailCommand, cancellationToken);
                if (result != Guid.Empty)
                {
                    var markReminderAsSentCommand = new MarkMaintenanceReminderAsSentCommand(email.Item1);
                    await mediator.Send(markReminderAsSentCommand, cancellationToken);
                }
            }

            //Wait for 1 hour before checking again
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }
}
