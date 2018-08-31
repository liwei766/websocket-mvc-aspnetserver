using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GPnaviServer.Migrations
{
    public partial class SensorMaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorMasters",
                columns: table => new
                {
                    SensorId = table.Column<string>(maxLength: 4, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 20, nullable: true),
                    SensorType = table.Column<string>(maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorMasters", x => x.SensorId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorMasters");
        }
    }
}
