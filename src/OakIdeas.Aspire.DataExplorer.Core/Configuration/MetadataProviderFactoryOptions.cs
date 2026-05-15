using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Configuration;

public sealed class MetadataProviderFactoryOptions
{
    private readonly List<ProviderRegistration> registrations = [];

    public IReadOnlyList<ProviderRegistration> Registrations => registrations;

    public void Register(DatabaseProviderType providerType, Type providerImplementationType)
    {
        ArgumentNullException.ThrowIfNull(providerImplementationType);

        if (!typeof(IMetadataProvider).IsAssignableFrom(providerImplementationType))
        {
            throw new ArgumentException(
                $"Provider type '{providerImplementationType.FullName}' must implement {nameof(IMetadataProvider)}.",
                nameof(providerImplementationType));
        }

        registrations.RemoveAll(registration => registration.ProviderType == providerType);
        registrations.Add(new ProviderRegistration(providerType, providerImplementationType));
    }
}
