using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sitko.Core.Apps.Blazor.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bar = table.Column<string>(type: "text", nullable: false),
                    FooId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sum = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageItem = table.Column<string>(type: "jsonb", nullable: true),
                    StorageItems = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Foos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Foo = table.Column<string>(type: "text", nullable: false),
                    BarModelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Foos_Bars_BarModelId",
                        column: x => x.BarModelId,
                        principalTable: "Bars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bars_FooId",
                table: "Bars",
                column: "FooId");

            migrationBuilder.CreateIndex(
                name: "IX_Foos_BarModelId",
                table: "Foos",
                column: "BarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Foos_FooId",
                table: "Bars",
                column: "FooId",
                principalTable: "Foos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Foos_FooId",
                table: "Bars");

            migrationBuilder.DropTable(
                name: "Foos");

            migrationBuilder.DropTable(
                name: "Bars");
        }
    }
}
