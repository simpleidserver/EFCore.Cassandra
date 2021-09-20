using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Cassandra.Samples.Migrations
{
    public partial class AddUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cv");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "cv",
                columns: table => new
                {
                    email = table.Column<string>(type: "text", nullable: false),
                    id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.email);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users",
                schema: "cv");

            migrationBuilder.EnsureSchema(
                name: "cv");
        }
    }
}
