using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Cassandra.Samples.Migrations
{
    public partial class AddEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cv");

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "cv",
                table: "applicants",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                schema: "cv",
                table: "applicants");

            migrationBuilder.EnsureSchema(
                name: "cv");
        }
    }
}
