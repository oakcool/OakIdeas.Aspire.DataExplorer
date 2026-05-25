namespace OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

/// <summary>
/// Provides SQL query templates for Object Explorer context menu actions.
/// </summary>
public static class ExplorerQueryTemplates
{
    /// <summary>
    /// Generates a SELECT TOP 1000 query for a table.
    /// </summary>
    public static string SelectTop1000(string schemaName, string tableName)
        => $"SELECT TOP 1000 *\nFROM [{schemaName}].[{tableName}]";

    /// <summary>
    /// Generates a templated INSERT statement for a table.
    /// </summary>
    public static string InsertStatement(string schemaName, string tableName)
        => $"INSERT INTO [{schemaName}].[{tableName}]\n(\n    Column1,\n    Column2\n)\nVALUES\n(\n    -- Column1,\n    -- Column2\n)";

    /// <summary>
    /// Generates a safe DELETE template for a table.
    /// </summary>
    public static string DeleteStatement(string schemaName, string tableName)
        => $"DELETE FROM [{schemaName}].[{tableName}]\nWHERE 1 = 0";

    /// <summary>
    /// Generates a TRUNCATE TABLE statement for a table.
    /// </summary>
    public static string TruncateStatement(string schemaName, string tableName)
        => $"TRUNCATE TABLE [{schemaName}].[{tableName}]";

    /// <summary>
    /// Generates a script definition query for any object using sp_helptext.
    /// </summary>
    public static string ScriptDefinition(string schemaName, string objectName)
        => $"EXEC sp_helptext '[{schemaName}].[{objectName}]'";

    /// <summary>
    /// Generates a templated EXEC statement for a stored procedure.
    /// </summary>
    public static string ExecuteProcedure(string schemaName, string procedureName)
        => $"EXEC [{schemaName}].[{procedureName}]";
}
