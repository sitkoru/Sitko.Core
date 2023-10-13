using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MudBlazorUnited.Migrations
{
    public partial class AddFooToBar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FooId",
                table: "Bars",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bars_FooId",
                table: "Bars",
                column: "FooId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Foos_FooId",
                table: "Bars",
                column: "FooId",
                principalTable: "Foos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Foos_FooId",
                table: "Bars");

            migrationBuilder.DropIndex(
                name: "IX_Bars_FooId",
                table: "Bars");

            migrationBuilder.DropColumn(
                name: "FooId",
                table: "Bars");
        }
    }
}
