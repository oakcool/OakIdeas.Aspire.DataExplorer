namespace OakIdeas.Aspire.DataExplorer.Web.Components.Pages;

public static class FeatureFlagAnchorBuilder
{
    public static string Build(string featureKey)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
        {
            return "feature-flag";
        }

        var chars = featureKey.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        var normalized = new string(chars);
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        return $"feature-flag-{normalized.Trim('-')}";
    }
}
