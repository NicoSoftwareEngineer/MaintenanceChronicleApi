using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MaintenanceChronicle.Infrastructure.DependencyInjection;

public static class ServiceInstallerExtensions
{
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
