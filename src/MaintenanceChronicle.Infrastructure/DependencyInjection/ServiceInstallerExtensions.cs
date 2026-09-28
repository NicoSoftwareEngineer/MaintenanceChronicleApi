using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MaintenanceChronicle.Infrastructure.DependencyInjection;

/// <summary>
/// Provides service registration through discovered installers.
/// </summary>
public static class ServiceInstallerExtensions
{
    /// <summary>
    /// Finds installers in matching assemblies in the given directory and runs them in order.
    /// </summary>
    /// <param name="services">Service collection to add services to.</param>
    /// <param name="configuration">Application configuration passed to each installer.</param>
    /// <param name="basePath">Directory containing the assemblies to inspect.</param>
    /// <returns>The service collection with the discovered services registered.</returns>
    public static IServiceCollection InstallServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string basePath)
    {
        //The SearchOption.TopDirectoryOnly may be a problem with Submodules or a different directory structure.
        var assemblyFiles = Directory.GetFiles(basePath, "*.dll", SearchOption.TopDirectoryOnly);

        var assemblies = new List<Assembly>();

        foreach (var file in assemblyFiles)
        {
            // Avoid loading system/framework assemblies, and only load assemblies that match the current assembly's name prefix.
            if (!Path.GetFileName(file).StartsWith(Assembly.GetExecutingAssembly().ManifestModule.Name.Split(".")[0]))
                continue;

            var assembly = Assembly.LoadFrom(file);
            assemblies.Add(assembly);
        }

        var installers = assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(IsAssignableToType<IServiceInstaller>)
            .Select(Activator.CreateInstance)
            .Cast<IServiceInstaller>()
            .ToArray();

        //Important: installers must be used in a specific order.
        installers = installers
            .OrderBy(i => i.Order).ToArray();

        foreach (var installer in installers)
        {
            installer.Install(services, configuration);
        }

        return services;
    }

    private static bool IsAssignableToType<T>(TypeInfo typeInfo) =>
        typeof(T).IsAssignableFrom(typeInfo) &&
        !typeInfo.IsInterface &&
        !typeInfo.IsAbstract;
}
