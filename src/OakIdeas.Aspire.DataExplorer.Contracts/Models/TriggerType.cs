using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

[Flags]
public enum TriggerType
{
    None = 0,
    Insert = 1,
    Update = 2,
    Delete = 4,
    InsteadOf = 8,
    After = 16,
}
