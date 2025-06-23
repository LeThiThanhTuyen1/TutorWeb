using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class addIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_delete",
                table: "Users",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "is_delete",
                table: "Courses",
                newName: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Users",
                newName: "is_delete");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "Courses",
                newName: "is_delete");
        }
    }
}
