using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCore.API.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Items",
                table: "Prescriptions",
                newName: "MedicinesJson");

            migrationBuilder.RenameColumn(
                name: "Tests",
                table: "LabOrders",
                newName: "TestType");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "Bills",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Advice",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispensedAt",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDispensed",
                table: "Prescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LabOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultNotes",
                table: "LabOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1484));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1488));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1490));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1491));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1493));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1494));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1495));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1496));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1498));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 30, 58, 915, DateTimeKind.Utc).AddTicks(1499));

            migrationBuilder.CreateIndex(
                name: "IX_Bills_AppointmentId",
                table: "Bills",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Appointments_AppointmentId",
                table: "Bills",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Appointments_AppointmentId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_AppointmentId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Advice",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DispensedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "IsDispensed",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "ResultNotes",
                table: "LabOrders");

            migrationBuilder.RenameColumn(
                name: "MedicinesJson",
                table: "Prescriptions",
                newName: "Items");

            migrationBuilder.RenameColumn(
                name: "TestType",
                table: "LabOrders",
                newName: "Tests");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Bills",
                newName: "PaymentStatus");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3099));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3104));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3105));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3106));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3108));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3109));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3110));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3111));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3112));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 11, 5, 51, 191, DateTimeKind.Utc).AddTicks(3115));
        }
    }
}
