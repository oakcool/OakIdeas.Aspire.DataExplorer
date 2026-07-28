namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum ErrorCategory
{
    ResourceNotFound = 1,
    ConnectionFailed = 2,
    QueryTimeout = 3,
    PermissionDenied = 4,
    ProviderError = 5,
    UnknownError = 6,
    FeatureDisabled = 7,
}

