using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Data;

public class DatabaseInstaller : IServiceInstaller
{
    public int Order => 2;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        //DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DbConnection"), optionsBuilder =>
            {
                optionsBuilder.UseNodaTime();
                optionsBuilder.MapEnum<RecordType>("recordType");
            });
        });
    }
}