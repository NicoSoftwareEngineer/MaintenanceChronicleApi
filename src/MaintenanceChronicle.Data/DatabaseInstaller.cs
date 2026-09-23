using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Data.Repositories;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using MaintenanceChronicle.Infrastructure.Persistence;
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IReadOnlyRepository<>), typeof(GenericReadOnlyRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
    }
}