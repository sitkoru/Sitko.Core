using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MudBlazorUnited.Tasks.Demo;

#nullable disable

namespace MudBlazorUnited.Tasks.Migrations
{
    /// <inheritdoc />
    public partial class Tasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Config = table.Column<LoggingTaskConfig>(type: "jsonb", nullable: true),
                    Result = table.Column<LoggingTaskResult>(type: "jsonb", nullable: true),
                    DateAdded = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TaskStatus = table.Column<int>(type: "integer", nullable: false),
                    ExecuteDateStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecuteDateEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    LastActivityDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
