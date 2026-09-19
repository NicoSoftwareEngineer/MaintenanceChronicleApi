using MaintenanceChronicle.BackgroundServices.BackgroundWorkers;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.BackgroundServices;

public class BackgroundServicesInstaller : IServiceInstaller
{
    public int Order => 5;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        //Adding EmailSenderBackgroundService into HostedServices
        services.AddHostedService<EmailSenderBackgroundService>();

        //Adding MaintenanceReminderBackgroundService into HostedServices
        services.AddHostedService<MaintenanceReminderBackgroundService>();
    }
}