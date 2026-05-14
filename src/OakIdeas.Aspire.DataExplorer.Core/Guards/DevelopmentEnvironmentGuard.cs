namespace OakIdeas.Aspire.DataExplorer.Core.Guards;

public static class DevelopmentEnvironmentGuard
{
    public static void EnsureDevelopment(bool isDevelopment, string message)
    {
        if (!isDevelopment)
        {
            throw new InvalidOperationException(message);
        }
    }
}
