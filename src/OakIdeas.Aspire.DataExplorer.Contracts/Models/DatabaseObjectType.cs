using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum DatabaseObjectType
{
    Unknown = 0,
    Schema = 1,
    Table = 2,
    View = 3,
    Procedure = 4,
    Function = 5,
    Trigger = 6,
    Index = 7,
}

