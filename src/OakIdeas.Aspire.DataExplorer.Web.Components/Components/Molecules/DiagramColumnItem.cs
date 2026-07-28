namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Represents a single column within a <see cref="DiagramEntityNode"/>.
/// </summary>
public sealed record DiagramColumnItem(
    string Name,
    string DataType,
    bool IsPrimaryKey,
    bool IsForeignKey,
    bool IsNullable,
    bool IsIdentity);

