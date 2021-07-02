using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Metadata.Postgres.DB;

namespace Sitko.Core.Storage.Metadata.Postgres.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorageItemRecords",
                schema: StorageDbContext.Schema,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Storage = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<StorageItemMetadata>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageItemRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageItemRecords");
        }
    }
}
