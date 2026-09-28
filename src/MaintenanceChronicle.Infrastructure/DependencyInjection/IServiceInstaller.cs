using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceChronicle.Infrastructure.DependencyInjection;

/// <summary>
/// Registers services for one application component.
/// </summary>
public interface IServiceInstaller
{
    /// <summary>
    /// Gets the order in which this installer runs.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Adds the component's services to the service collection.
    /// </summary>
    /// <param name="services">Service collection to add services to.</param>
    /// <param name="configuration">Application configuration available to the installer.</param>
    void Install(IServiceCollection services, IConfiguration configuration);
}