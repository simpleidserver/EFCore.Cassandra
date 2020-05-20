using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using Cassandra;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Cassandra.Samples.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cv");

            migrationBuilder.CreateTable(
                name: "applicants",
                schema: "cv",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    LastName = table.Column<string>(nullable: false),
                    Long = table.Column<long>(nullable: false),
                    Bool = table.Column<bool>(nullable: false),
                    Decimal = table.Column<decimal>(nullable: false),
                    Double = table.Column<double>(nullable: false),
                    Float = table.Column<float>(nullable: false),
                    Integer = table.Column<int>(nullable: false),
                    SmallInt = table.Column<short>(nullable: false),
                    DateTimeOffset = table.Column<DateTimeOffset>(nullable: false),
                    TimeUuid = table.Column<Guid>(nullable: false),
                    Sbyte = table.Column<sbyte>(nullable: false),
                    BigInteger = table.Column<BigInteger>(nullable: false),
                    Blob = table.Column<byte[]>(nullable: true),
                    LocalDate = table.Column<LocalDate>(nullable: true),
                    Ip = table.Column<IPAddress>(nullable: true),
                    LocalTime = table.Column<LocalTime>(nullable: true),
                    Lst = table.Column<IEnumerable<string>>(nullable: true),
                    LstInt = table.Column<IEnumerable<int>>(nullable: true),
                    Dic = table.Column<IDictionary<string, string>>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applicants", x => new { x.id, x.LastName });
                });

            migrationBuilder.CreateTable(
                name: "cvs",
                schema: "cv",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cvs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "applicants",
                schema: "cv");

            migrationBuilder.DropTable(
                name: "cvs",
                schema: "cv");
        }
    }
}
