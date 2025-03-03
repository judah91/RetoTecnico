using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransactionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFraudResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Solo se crea la tabla `FraudResults`
            migrationBuilder.CreateTable(
                name: "FraudResults",
                columns: table => new
                {
                    TransactionExternalId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFraudulent = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudResults", x => x.TransactionExternalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ❌ Solo eliminamos `FraudResults` en caso de rollback
            migrationBuilder.DropTable(
                name: "FraudResults");
        }
    }
}