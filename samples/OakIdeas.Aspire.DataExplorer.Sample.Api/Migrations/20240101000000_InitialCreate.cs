using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

#nullable disable

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Migrations;

/// <inheritdoc />
[DbContext(typeof(SampleDbContext))]
[Migration("20240101000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TodoItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItems", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TodoItems");
    }
}
