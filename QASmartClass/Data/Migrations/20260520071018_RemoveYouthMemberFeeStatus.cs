using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QASmartClass.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveYouthMemberFeeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyTasks_SchoolEvents_EventId",
                table: "DailyTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_TaskItems_TaskId",
                table: "TaskComments");

            migrationBuilder.DropIndex(
                name: "IX_UsageLogs_EventType",
                table: "UsageLogs");

            migrationBuilder.DropIndex(
                name: "IX_UsageLogs_Timestamp",
                table: "UsageLogs");

            migrationBuilder.DropIndex(
                name: "IX_TaskComments_TaskId",
                table: "TaskComments");

            migrationBuilder.DropIndex(
                name: "IX_DailyTasks_EventId",
                table: "DailyTasks");

            migrationBuilder.DropColumn(
                name: "FeeStatus",
                table: "YouthMembers");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "YouthRecruitments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "ActivityId",
                table: "YouthEmulationScores",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "QuestionBankCategories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "QuestionBankCategories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "QuestionBankCategories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "QuestionBankCategories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "QuestionBankCategories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "YouthRecruitments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeStatus",
                table: "YouthMembers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "ActivityId",
                table: "YouthEmulationScores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_EventType",
                table: "UsageLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Timestamp",
                table: "UsageLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId",
                table: "TaskComments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_EventId",
                table: "DailyTasks",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyTasks_SchoolEvents_EventId",
                table: "DailyTasks",
                column: "EventId",
                principalTable: "SchoolEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_TaskItems_TaskId",
                table: "TaskComments",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

