using Microsoft.EntityFrameworkCore.Migrations;

namespace Sitko.Core.Apps.Blazor.Migrations
{
    public partial class AddSumToBar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Sum",
                table: "Bars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sum",
                table: "Bars");
        }
    }
}
