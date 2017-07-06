using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tushino.Migrations
{
    public partial class CommAndWinner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Admin",
                table: "Replays",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommanderEast",
                table: "Replays",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommanderGuer",
                table: "Replays",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommanderWest",
                table: "Replays",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayTime",
                table: "Replays",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WinnerSide",
                table: "Replays",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Admin",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "CommanderEast",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "CommanderGuer",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "CommanderWest",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "PlayTime",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "WinnerSide",
                table: "Replays");
        }
    }
}
