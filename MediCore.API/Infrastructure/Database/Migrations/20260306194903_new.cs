using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MediCore.API.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: false),
                    ConsultationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MorningStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    MorningEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    HasEveningShift = table.Column<bool>(type: "bit", nullable: false),
                    EveningStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    EveningEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxPatientsPerDay = table.Column<int>(type: "int", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorProfiles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "Description", "FloorNumber", "Icon", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4389), "General health consultations", 1, "🏥", true, "General Medicine" },
                    { 2, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4399), "Heart care", 2, "❤️", true, "Cardiology" },
                    { 3, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4403), "Brain care", 2, "🧠", true, "Neurology" },
                    { 4, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4406), "Bone care", 3, "🦴", true, "Orthopedics" },
                    { 5, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4409), "Children care", 1, "👶", true, "Pediatrics" },
                    { 6, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4412), "Women health", 4, "🌸", true, "Gynecology" },
                    { 7, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4415), "Skin care", 2, "✨", true, "Dermatology" },
                    { 8, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4418), "Eye care", 1, "👁️", true, "Ophthalmology" },
                    { 9, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4420), "Ear nose throat", 3, "👂", true, "ENT" },
                    { 10, new DateTime(2026, 3, 6, 19, 48, 59, 284, DateTimeKind.Utc).AddTicks(4423), "Mental health", 4, "🧘", true, "Psychiatry" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_DepartmentId",
                table: "DoctorProfiles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_UserId",
                table: "DoctorProfiles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProfiles");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
