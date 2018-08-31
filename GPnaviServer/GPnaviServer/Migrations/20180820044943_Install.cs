using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GPnaviServer.Migrations
{
    public partial class Install : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HolidayMasters",
                columns: table => new
                {
                    Holiday = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayMasters", x => x.Holiday);
                });

            migrationBuilder.CreateTable(
                name: "SensorMonitors",
                columns: table => new
                {
                    SensorId = table.Column<string>(maxLength: 4, nullable: false),
                    LastReceiveTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorMonitors", x => x.SensorId);
                });

            migrationBuilder.CreateTable(
                name: "SensorStatuses",
                columns: table => new
                {
                    SensorId = table.Column<string>(maxLength: 4, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 20, nullable: true),
                    LoginId = table.Column<string>(maxLength: 13, nullable: true),
                    OccurrenceDate = table.Column<DateTime>(nullable: false),
                    SensorType = table.Column<string>(maxLength: 8, nullable: true),
                    StartDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorStatuses", x => x.SensorId);
                });

            migrationBuilder.CreateTable(
                name: "UserMasters",
                columns: table => new
                {
                    LoginId = table.Column<string>(maxLength: 13, nullable: false),
                    IsValid = table.Column<bool>(nullable: false),
                    LoginName = table.Column<string>(maxLength: 16, nullable: true),
                    Password = table.Column<string>(maxLength: 64, nullable: true),
                    RemoveDate = table.Column<DateTime>(nullable: false),
                    Role = table.Column<string>(maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMasters", x => x.LoginId);
                });

            migrationBuilder.CreateTable(
                name: "UserStatuses",
                columns: table => new
                {
                    LoginId = table.Column<string>(maxLength: 13, nullable: false),
                    DeviceToken = table.Column<string>(maxLength: 4096, nullable: true),
                    DeviceType = table.Column<string>(maxLength: 8, nullable: true),
                    SessionKey = table.Column<string>(maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatuses", x => x.LoginId);
                });

            migrationBuilder.CreateTable(
                name: "UserVersions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ExpirationDate = table.Column<DateTime>(nullable: false),
                    RegisterDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleMasters",
                columns: table => new
                {
                    Version = table.Column<long>(nullable: false),
                    Start = table.Column<string>(maxLength: 5, nullable: false),
                    Name = table.Column<string>(maxLength: 40, nullable: false),
                    Holiday = table.Column<string>(maxLength: 1, nullable: false),
                    IconId = table.Column<string>(maxLength: 4, nullable: true),
                    Priority = table.Column<string>(maxLength: 1, nullable: true),
                    Row = table.Column<long>(nullable: false),
                    ShortName = table.Column<string>(maxLength: 20, nullable: true),
                    Time = table.Column<string>(maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleMasters", x => new { x.Version, x.Start, x.Name, x.Holiday });
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStatuses",
                columns: table => new
                {
                    Version = table.Column<long>(nullable: false),
                    Start = table.Column<string>(maxLength: 5, nullable: false),
                    Name = table.Column<string>(maxLength: 40, nullable: false),
                    Holiday = table.Column<string>(maxLength: 1, nullable: false),
                    LoginId = table.Column<string>(maxLength: 13, nullable: true),
                    StartDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(maxLength: 8, nullable: true),
                    StatusUpdateDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStatuses", x => new { x.Version, x.Start, x.Name, x.Holiday });
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleVersions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ExpirationDate = table.Column<DateTime>(nullable: false),
                    RegisterDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkStatusHistories",
                columns: table => new
                {
                    Version = table.Column<long>(nullable: false),
                    Start = table.Column<string>(maxLength: 5, nullable: false),
                    Name = table.Column<string>(maxLength: 40, nullable: false),
                    Holiday = table.Column<string>(maxLength: 1, nullable: false),
                    SensorId = table.Column<string>(maxLength: 4, nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false),
                    DisplayName = table.Column<string>(maxLength: 20, nullable: true),
                    LoginId = table.Column<string>(maxLength: 13, nullable: true),
                    LoginName = table.Column<string>(maxLength: 16, nullable: true),
                    RegisterDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkStatusHistories", x => new { x.Version, x.Start, x.Name, x.Holiday, x.SensorId, x.StartDate });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayMasters");

            migrationBuilder.DropTable(
                name: "SensorMonitors");

            migrationBuilder.DropTable(
                name: "SensorStatuses");

            migrationBuilder.DropTable(
                name: "UserMasters");

            migrationBuilder.DropTable(
                name: "UserStatuses");

            migrationBuilder.DropTable(
                name: "UserVersions");

            migrationBuilder.DropTable(
                name: "WorkScheduleMasters");

            migrationBuilder.DropTable(
                name: "WorkScheduleStatuses");

            migrationBuilder.DropTable(
                name: "WorkScheduleVersions");

            migrationBuilder.DropTable(
                name: "WorkStatusHistories");
        }
    }
}
