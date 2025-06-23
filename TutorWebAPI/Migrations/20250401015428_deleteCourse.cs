using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class deleteCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Contracts_contract_id",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Users_user_id",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Courses_course_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Students_student_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Tutors_tutor_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Students_student_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Students_student_id",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Tutors_tutor_id",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Courses_course_id",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tutors_tutor_id",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "BlacklistedTokens");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpiry",
                table: "Users",
                newName: "refresh_token_expiry");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "Users",
                newName: "refresh_token");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Contracts_contract_id",
                table: "Complaints",
                column: "contract_id",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Users_user_id",
                table: "Complaints",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Courses_course_id",
                table: "Contracts",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Students_student_id",
                table: "Contracts",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Tutors_tutor_id",
                table: "Contracts",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Students_student_id",
                table: "Enrollments",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Students_student_id",
                table: "Feedbacks",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Tutors_tutor_id",
                table: "Feedbacks",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Courses_course_id",
                table: "Schedules",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tutors_tutor_id",
                table: "Schedules",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Contracts_contract_id",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Users_user_id",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Courses_course_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Students_student_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Tutors_tutor_id",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Students_student_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Students_student_id",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Tutors_tutor_id",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Courses_course_id",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tutors_tutor_id",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "refresh_token_expiry",
                table: "Users",
                newName: "RefreshTokenExpiry");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "Users",
                newName: "RefreshToken");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "BlacklistedTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Contracts_contract_id",
                table: "Complaints",
                column: "contract_id",
                principalTable: "Contracts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Users_user_id",
                table: "Complaints",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Courses_course_id",
                table: "Contracts",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Students_student_id",
                table: "Contracts",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Tutors_tutor_id",
                table: "Contracts",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Students_student_id",
                table: "Enrollments",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Students_student_id",
                table: "Feedbacks",
                column: "student_id",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Tutors_tutor_id",
                table: "Feedbacks",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Courses_course_id",
                table: "Schedules",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tutors_tutor_id",
                table: "Schedules",
                column: "tutor_id",
                principalTable: "Tutors",
                principalColumn: "Id");
        }
    }
}
