using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class removeVerifyEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_code",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "verification_expiry",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_expiry",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }
    }
}
