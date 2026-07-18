using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Core.Guards;

namespace OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

public static class DataExplorerHostingExtensions
{
    private const string DevelopmentOnlyMessage = "AddDataExplorer can only be used in Development environments.";
    private const string WebRuntimeDirectoryName = "data-explorer-web";
    private const string WebAssemblyFileName = "OakIdeas.Aspire.DataExplorer.Web.dll";
    private const string WebExecutableFileName = "OakIdeas.Aspire.DataExplorer.Web.exe";

    public static IResourceBuilder<ExecutableResource> AddDataExplorer(
        this IDistributedApplicationBuilder builder)
    {
        return AddDataExplorer(builder, runtimeDirectory: null);
    }

    public static IResourceBuilder<ExecutableResource> AddDataExplorer(
        this IDistributedApplicationBuilder builder,
        string? runtimeDirectory)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(builder.Environment.IsDevelopment(), DevelopmentOnlyMessage);

        runtimeDirectory = ResolveRuntimeDirectory(runtimeDirectory);
        var packagedAppPath = Path.Combine(runtimeDirectory, WebAssemblyFileName);
        var packagedExecutablePath = Path.Combine(runtimeDirectory, WebExecutableFileName);

        // In local project-reference development, discover the publish output from source if
        // the packaged runtime folder has not been copied into the consumer output.
        if (!File.Exists(packagedAppPath) && !File.Exists(packagedExecutablePath))
        {
            var developmentRuntimeDirectory = TryResolveDevelopmentRuntimeDirectory(runtimeDirectory);
            if (!string.IsNullOrWhiteSpace(developmentRuntimeDirectory))
            {
                runtimeDirectory = developmentRuntimeDirectory;
                packagedAppPath = Path.Combine(runtimeDirectory, WebAssemblyFileName);
                packagedExecutablePath = Path.Combine(runtimeDirectory, WebExecutableFileName);
            }
        }

        // Fall back to the legacy output layout for existing local builds/packages.
        if (!File.Exists(packagedAppPath) && !File.Exists(packagedExecutablePath))
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(DataExplorerHostingExtensions).Assembly.Location)
                ?? throw new InvalidOperationException("Unable to determine the Data Explorer hosting assembly location.");

            runtimeDirectory = assemblyDirectory;
            packagedAppPath = Path.Combine(runtimeDirectory, WebAssemblyFileName);
            packagedExecutablePath = Path.Combine(runtimeDirectory, WebExecutableFileName);
        }

        var resource = File.Exists(packagedAppPath)
            ? builder.AddExecutable("data-explorer", "dotnet", runtimeDirectory, WebAssemblyFileName)
            : File.Exists(packagedExecutablePath)
                ? builder.AddExecutable("data-explorer", $".\\{WebExecutableFileName}", runtimeDirectory)
                : throw new InvalidOperationException(
                    $"The packaged Data Explorer app was not found in '{runtimeDirectory}'. Ensure the package assets were copied to the application output.");

        // Bind to loopback only and do NOT publish an external endpoint. Data Explorer runs
        // arbitrary developer SQL with no authentication, so it must never be reachable from
        // other hosts on the network. Consumers that deliberately need remote access can add
        // `.WithExternalHttpEndpoints()` themselves.
        return resource
            .WithHttpEndpoint(env: "HTTP_PORTS")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");
    }

    private static string ResolveRuntimeDirectory(string? runtimeDirectory)
    {
        if (!string.IsNullOrWhiteSpace(runtimeDirectory))
        {
            return Path.GetFullPath(runtimeDirectory);
        }

        var assemblyDirectory = Path.GetDirectoryName(typeof(DataExplorerHostingExtensions).Assembly.Location)
            ?? throw new InvalidOperationException("Unable to determine the Data Explorer hosting assembly location.");

        return Path.Combine(assemblyDirectory, WebRuntimeDirectoryName);
    }

    private static string? TryResolveDevelopmentRuntimeDirectory(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "OakIdeas.Aspire.DataExplorer.Hosting",
                "DataExplorerWeb",
                "publish");

            var candidateAppPath = Path.Combine(candidate, WebAssemblyFileName);
            var candidateExecutablePath = Path.Combine(candidate, WebExecutableFileName);

            if (File.Exists(candidateAppPath) || File.Exists(candidateExecutablePath))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
