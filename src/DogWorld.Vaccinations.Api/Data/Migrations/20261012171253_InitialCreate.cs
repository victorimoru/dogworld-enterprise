using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogWorld.Vaccinations.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vaccinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DogId = table.Column<int>(type: "int", nullable: false),
                    VaccineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdministeredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    NextDueOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vaccinations", x => x.Id);
                    table.CheckConstraint("CK_Vaccinations_Dates", "[NextDueOn] IS NULL OR [NextDueOn] >= [AdministeredOn]");
                    table.CheckConstraint("CK_Vaccinations_DogId", "[DogId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vaccinations_DogId_AdministeredOn_Id",
                table: "Vaccinations",
                columns: new[] { "DogId", "AdministeredOn", "Id" },
                descending: new[] { false, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vaccinations");
        }
    }
}
