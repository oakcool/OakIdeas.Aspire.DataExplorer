namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum SchemaMigrationState
{
    Applied = 0,
    Pending = 1,
    MissingFromDatabase = 2,
    MissingFromProject = 3,
    OutOfOrder = 4,
}
