using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WASMDemo.Shared.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            var i = 0;
            while (i < 50)
            {
                migrationBuilder.InsertData(table: "Entities", columns: new []{"Id", "Text"}, values: new []{Guid.NewGuid().ToString(), $"Test {i}"});
                i++;
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entities");
        }
    }
}
