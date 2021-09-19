using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Cassandra.Samples.Migrations
{
    public partial class NullableInteger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cv");

            migrationBuilder.AddColumn<int>(
                name: "NullableInteger",
                schema: "cv",
                table: "applicants",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NullableInteger",
                schema: "cv",
                table: "applicants");

            migrationBuilder.EnsureSchema(
                name: "cv");
        }
    }
}
