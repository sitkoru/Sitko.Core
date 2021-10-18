using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sitko.Core.Apps.Blazor.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bar = table.Column<string>(type: "text", nullable: false),
                    StorageItem = table.Column<string>(type: "jsonb", nullable: true)
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foos_BarModelId",
                table: "Foos",
                column: "BarModelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foos");

            migrationBuilder.DropTable(
                name: "Bars");
        }
    }
}
