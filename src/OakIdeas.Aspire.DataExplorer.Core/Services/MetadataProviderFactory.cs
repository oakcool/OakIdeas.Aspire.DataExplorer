using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class MetadataProviderFactory(
    IServiceProvider serviceProvider,
    IOptions<MetadataProviderFactoryOptions> options) : IProviderFactory
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly IReadOnlyDictionary<DatabaseProviderType, ProviderRegistration> registrations = options.Value.Registrations
        .GroupBy(registration => registration.ProviderType)
        .ToDictionary(group => group.Key, group => group.Last());

    public IMetadataProvider Create(DatabaseProviderType providerType)
        => TryCreate(providerType, out var provider)
            ? provider!
            : throw new InvalidOperationException($"No metadata provider is registered for '{providerType}'.");

    public bool TryCreate(DatabaseProviderType providerType, out IMetadataProvider? provider)
    {
        provider = null;

        if (!registrations.TryGetValue(providerType, out var registration))
        {
            return false;
        }

        var resolved = serviceProvider.GetService(registration.ProviderImplementationType) as IMetadataProvider;

        if (resolved is null)
        {
            throw new InvalidOperationException(
                $"Metadata provider '{registration.ProviderImplementationType.FullName}' is not registered in the service provider.");
        }

        if (resolved.ProviderType != providerType)
        {
            throw new InvalidOperationException(
                $"Metadata provider '{registration.ProviderImplementationType.FullName}' is registered for '{providerType}' but reports '{resolved.ProviderType}'.");
        }

        provider = resolved;
        return true;
    }
}
