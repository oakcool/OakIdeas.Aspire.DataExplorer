using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Migrations
{
    /// <inheritdoc />
    public partial class ShowcaseProgrammabilityObjects : Migration
    {
        private const string ShowcaseSchema = "showcase";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: ShowcaseSchema);

            migrationBuilder.CreateTable(
                name: "TodoListsReplica",
                schema: ShowcaseSchema,
                columns: table => new
                {
                    TodoListId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoListsReplica", x => x.TodoListId);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemsReplica",
                schema: ShowcaseSchema,
                columns: table => new
                {
                    TodoItemId = table.Column<int>(type: "int", nullable: false),
                    TodoListId = table.Column<int>(type: "int", nullable: false),
                    TodoStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    TodoPriorityId = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemsReplica", x => x.TodoItemId);
                    table.ForeignKey(
                        name: "FK_TodoItemsReplica_TodoListsReplica_TodoListId",
                        column: x => x.TodoListId,
                        principalSchema: ShowcaseSchema,
                        principalTable: "TodoListsReplica",
                        principalColumn: "TodoListId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemsReplica_TodoListId",
                schema: ShowcaseSchema,
                table: "TodoItemsReplica",
                column: "TodoListId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemsReplica_TodoStatusId",
                schema: ShowcaseSchema,
                table: "TodoItemsReplica",
                column: "TodoStatusId");

            migrationBuilder.Sql($"""
                INSERT INTO [{ShowcaseSchema}].[TodoListsReplica] ([TodoListId], [Name], [Description], [IsArchived], [CreatedAt])
                SELECT [Id], [Name], [Description], [IsArchived], [CreatedAt]
                FROM [dbo].[TodoLists];
                """);

            migrationBuilder.Sql($"""
                INSERT INTO [{ShowcaseSchema}].[TodoItemsReplica] ([TodoItemId], [TodoListId], [TodoStatusId], [TodoPriorityId], [Title], [Description], [IsArchived], [CreatedAt])
                SELECT [Id], [TodoListId], [TodoStatusId], [TodoPriorityId], [Title], [Description], [IsArchived], [CreatedAt]
                FROM [dbo].[TodoItems];
                """);

            migrationBuilder.Sql($"""
                CREATE VIEW [{ShowcaseSchema}].[vwTodoReplicaOverview]
                AS
                SELECT
                    i.[TodoItemId],
                    i.[Title],
                    l.[Name] AS [ListName],
                    s.[Name] AS [StatusName],
                    p.[Name] AS [PriorityName],
                    i.[IsArchived],
                    i.[CreatedAt]
                FROM [{ShowcaseSchema}].[TodoItemsReplica] AS i
                INNER JOIN [{ShowcaseSchema}].[TodoListsReplica] AS l
                    ON l.[TodoListId] = i.[TodoListId]
                INNER JOIN [dbo].[TodoStatuses] AS s
                    ON s.[Id] = i.[TodoStatusId]
                INNER JOIN [dbo].[TodoPriorities] AS p
                    ON p.[Id] = i.[TodoPriorityId];
                """);

            migrationBuilder.Sql($"""
                CREATE FUNCTION [{ShowcaseSchema}].[ufn_OpenReplicaTodoCount]()
                RETURNS INT
                AS
                BEGIN
                    DECLARE @result INT;

                    SELECT @result = COUNT_BIG(1)
                    FROM [{ShowcaseSchema}].[TodoItemsReplica] AS i
                    INNER JOIN [dbo].[TodoStatuses] AS s
                        ON s.[Id] = i.[TodoStatusId]
                    WHERE i.[IsArchived] = 0
                      AND s.[IsClosed] = 0;

                    RETURN CAST(@result AS INT);
                END;
                """);

            migrationBuilder.Sql($"""
                CREATE PROCEDURE [{ShowcaseSchema}].[usp_ListReplicaTodosByStatus]
                    @StatusId TINYINT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        i.[TodoItemId],
                        i.[Title],
                        l.[Name] AS [ListName],
                        s.[Name] AS [StatusName],
                        p.[Name] AS [PriorityName],
                        i.[CreatedAt]
                    FROM [{ShowcaseSchema}].[TodoItemsReplica] AS i
                    INNER JOIN [{ShowcaseSchema}].[TodoListsReplica] AS l
                        ON l.[TodoListId] = i.[TodoListId]
                    INNER JOIN [dbo].[TodoStatuses] AS s
                        ON s.[Id] = i.[TodoStatusId]
                    INNER JOIN [dbo].[TodoPriorities] AS p
                        ON p.[Id] = i.[TodoPriorityId]
                    WHERE @StatusId IS NULL OR i.[TodoStatusId] = @StatusId
                    ORDER BY i.[TodoItemId];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DROP PROCEDURE IF EXISTS [{ShowcaseSchema}].[usp_ListReplicaTodosByStatus];
                """);

            migrationBuilder.Sql($"""
                DROP FUNCTION IF EXISTS [{ShowcaseSchema}].[ufn_OpenReplicaTodoCount];
                """);

            migrationBuilder.Sql($"""
                DROP VIEW IF EXISTS [{ShowcaseSchema}].[vwTodoReplicaOverview];
                """);

            migrationBuilder.DropTable(
                name: "TodoItemsReplica",
                schema: ShowcaseSchema);

            migrationBuilder.DropTable(
                name: "TodoListsReplica",
                schema: ShowcaseSchema);
        }
    }
}
