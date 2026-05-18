using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Migrations
{
    /// <inheritdoc />
    public partial class ElevateTodoSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "TodoItems");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TodoItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "TodoItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TodoCategoryId",
                table: "TodoItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TodoListId",
                table: "TodoItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte>(
                name: "TodoPriorityId",
                table: "TodoItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)2);

            migrationBuilder.AddColumn<byte>(
                name: "TodoStatusId",
                table: "TodoItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TodoCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValue: "#64748b"),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TodoItemId = table.Column<int>(type: "int", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoComments_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoPriorities",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoPriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoStatuses",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemTags",
                columns: table => new
                {
                    TodoItemId = table.Column<int>(type: "int", nullable: false),
                    TodoTagId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemTags", x => new { x.TodoItemId, x.TodoTagId });
                    table.ForeignKey(
                        name: "FK_TodoItemTags_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemTags_TodoTags_TodoTagId",
                        column: x => x.TodoTagId,
                        principalTable: "TodoTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TodoCategories",
                columns: new[] { "Id", "ColorHex", "IsSystem", "Name" },
                values: new object[] { 1, "#2563eb", true, "Engineering" });

            migrationBuilder.InsertData(
                table: "TodoCategories",
                columns: new[] { "Id", "ColorHex", "Name" },
                values: new object[,]
                {
                    { 2, "#16a34a", "Household" },
                    { 3, "#7c3aed", "Learning" }
                });

            migrationBuilder.InsertData(
                table: "TodoLists",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tasks related to DataExplorer development", "Work" },
                    { 2, new DateTimeOffset(new DateTime(2026, 1, 5, 9, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Personal goals and errands", "Personal" }
                });

            migrationBuilder.InsertData(
                table: "TodoPriorities",
                columns: new[] { "Id", "IsDefault", "Name", "SortOrder" },
                values: new object[,]
                {
                    { (byte)1, false, "Low", (byte)1 },
                    { (byte)2, true, "Medium", (byte)2 },
                    { (byte)3, false, "High", (byte)3 },
                    { (byte)4, false, "Critical", (byte)4 }
                });

            migrationBuilder.InsertData(
                table: "TodoStatuses",
                columns: new[] { "Id", "IsClosed", "Name", "SortOrder" },
                values: new object[,]
                {
                    { (byte)1, false, "Not Started", (byte)1 },
                    { (byte)2, false, "In Progress", (byte)2 },
                    { (byte)3, false, "Blocked", (byte)3 },
                    { (byte)4, true, "Completed", (byte)4 }
                });

            migrationBuilder.InsertData(
                table: "TodoTags",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Aspire integration", "aspire" },
                    { 2, "Entity Framework changes", "ef-core" },
                    { 3, "Documentation updates", "docs" },
                    { 4, "Needs immediate attention", "urgent" },
                    { 5, "API and service work", "backend" }
                });

            migrationBuilder.InsertData(
                table: "TodoItems",
                columns: new[] { "Id", "CompletedAt", "CreatedAt", "Description", "DueAt", "Title", "TodoCategoryId", "TodoListId", "TodoPriorityId", "TodoStatusId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTimeOffset(new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Add richer schema objects so DataExplorer shows meaningful relationships and constraints.", new DateTimeOffset(new DateTime(2026, 1, 15, 17, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instrument sample metadata discovery", 1, 1, (byte)3, (byte)2, new DateTimeOffset(new DateTime(2026, 1, 11, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, null, new DateTimeOffset(new DateTime(2026, 1, 11, 8, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Document local migration commands and seeded data expectations.", null, "Write migration walkthrough docs", 3, 1, (byte)2, (byte)1, null },
                    { 3, null, new DateTimeOffset(new DateTime(2026, 1, 12, 10, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refactor sample page styles", 1, 1, (byte)2, (byte)3, null },
                    { 4, new DateTimeOffset(new DateTime(2026, 1, 13, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 10, 7, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Buy groceries and prep for Monday.", new DateTimeOffset(new DateTime(2026, 1, 13, 13, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Plan weekend errands", 2, 2, (byte)1, (byte)4, new DateTimeOffset(new DateTime(2026, 1, 13, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "TodoComments",
                columns: new[] { "Id", "AuthorName", "Body", "CreatedAt", "TodoItemId" },
                values: new object[,]
                {
                    { 1, "System", "Kickoff item seeded for demo.", new DateTimeOffset(new DateTime(2026, 1, 10, 9, 10, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { 2, "Sam", "Remember to validate indexes and constraints in DataExplorer.", new DateTimeOffset(new DateTime(2026, 1, 11, 11, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { 3, "Alex", "Blocked waiting for color palette review.", new DateTimeOffset(new DateTime(2026, 1, 12, 10, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3 },
                    { 4, "Taylor", "Errands complete early.", new DateTimeOffset(new DateTime(2026, 1, 13, 12, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4 }
                });

            migrationBuilder.InsertData(
                table: "TodoItemTags",
                columns: new[] { "TodoItemId", "TodoTagId", "AddedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTimeOffset(new DateTime(2026, 1, 10, 9, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 1, 2, new DateTimeOffset(new DateTime(2026, 1, 10, 9, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, 3, new DateTimeOffset(new DateTime(2026, 1, 11, 8, 40, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, 4, new DateTimeOffset(new DateTime(2026, 1, 12, 10, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, 5, new DateTimeOffset(new DateTime(2026, 1, 12, 10, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, 2, new DateTimeOffset(new DateTime(2026, 1, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_CreatedAt",
                table: "TodoItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoCategoryId",
                table: "TodoItems",
                column: "TodoCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId_TodoStatusId_DueAt",
                table: "TodoItems",
                columns: new[] { "TodoListId", "TodoStatusId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoPriorityId",
                table: "TodoItems",
                column: "TodoPriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoStatusId",
                table: "TodoItems",
                column: "TodoStatusId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TodoItems_Title_NotBlank",
                table: "TodoItems",
                sql: "LEN(LTRIM(RTRIM([Title]))) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_TodoCategories_Name",
                table: "TodoCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoComments_TodoItemId_CreatedAt",
                table: "TodoComments",
                columns: new[] { "TodoItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemTags_TodoTagId",
                table: "TodoItemTags",
                column: "TodoTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_Name",
                table: "TodoLists",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoPriorities_Name",
                table: "TodoPriorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoStatuses_Name",
                table: "TodoStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_Name",
                table: "TodoTags",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoCategories_TodoCategoryId",
                table: "TodoItems",
                column: "TodoCategoryId",
                principalTable: "TodoCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoLists_TodoListId",
                table: "TodoItems",
                column: "TodoListId",
                principalTable: "TodoLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoPriorities_TodoPriorityId",
                table: "TodoItems",
                column: "TodoPriorityId",
                principalTable: "TodoPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoStatuses_TodoStatusId",
                table: "TodoItems",
                column: "TodoStatusId",
                principalTable: "TodoStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoCategories_TodoCategoryId",
                table: "TodoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoLists_TodoListId",
                table: "TodoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoPriorities_TodoPriorityId",
                table: "TodoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoStatuses_TodoStatusId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoCategories");

            migrationBuilder.DropTable(
                name: "TodoComments");

            migrationBuilder.DropTable(
                name: "TodoItemTags");

            migrationBuilder.DropTable(
                name: "TodoLists");

            migrationBuilder.DropTable(
                name: "TodoPriorities");

            migrationBuilder.DropTable(
                name: "TodoStatuses");

            migrationBuilder.DropTable(
                name: "TodoTags");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_CreatedAt",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoCategoryId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoListId_TodoStatusId_DueAt",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoPriorityId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoStatusId",
                table: "TodoItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TodoItems_Title_NotBlank",
                table: "TodoItems");

            migrationBuilder.DeleteData(
                table: "TodoItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TodoItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TodoItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TodoItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "TodoCategoryId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "TodoListId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "TodoPriorityId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "TodoStatusId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TodoItems");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "TodoItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
