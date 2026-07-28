using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record StoredProcedureParameterMetadata(
    string Name,
    string DataType,
    RoutineParameterDirection Direction = RoutineParameterDirection.Input,
    bool HasDefault = false);
