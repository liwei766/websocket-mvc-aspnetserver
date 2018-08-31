using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GPnaviServer.Migrations
{
    public partial class WorkStatusHistoryKeyChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkStatusHistories",
                table: "WorkStatusHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkStatusHistories",
                table: "WorkStatusHistories",
                columns: new[] { "Version", "Start", "Name", "Holiday", "SensorId", "RegisterDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkStatusHistories_StartDate",
                table: "WorkStatusHistories",
                column: "StartDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkStatusHistories",
                table: "WorkStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_WorkStatusHistories_StartDate",
                table: "WorkStatusHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkStatusHistories",
                table: "WorkStatusHistories",
                columns: new[] { "Version", "Start", "Name", "Holiday", "SensorId", "StartDate" });
        }
    }
}
