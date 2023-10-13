using Microsoft.EntityFrameworkCore.Migrations;

namespace MudBlazorUnited.Migrations
{
    public partial class AddStorageItemsToBar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageItems",
                table: "Bars",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageItems",
                table: "Bars");
        }
    }
}
