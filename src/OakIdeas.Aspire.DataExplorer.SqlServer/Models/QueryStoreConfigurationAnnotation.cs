using Aspire.Hosting.ApplicationModel;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Models;

internal sealed record QueryStoreConfigurationAnnotation(
    QueryStoreOptions Options) : IResourceAnnotation;
