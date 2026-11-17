using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Souqify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecValuesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSpecValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSpecValues", x => x.Id);
                    table.CheckConstraint("CK_ProductSpecValues_ExactlyOneValue", "num_nonnulls(\"OptionId\", \"NumberValue\", \"BoolValue\", \"TextValue\") = 1");
                    table.ForeignKey(
                        name: "FK_ProductSpecValues_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSpecValues_SpecAttributes_SpecAttributeId",
                        column: x => x.SpecAttributeId,
                        principalTable: "SpecAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSpecValues_SpecOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "SpecOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecValues_OptionId",
                table: "ProductSpecValues",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecValues_ProductId_SpecAttributeId_OptionId",
                table: "ProductSpecValues",
                columns: new[] { "ProductId", "SpecAttributeId", "OptionId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecValues_SpecAttributeId",
                table: "ProductSpecValues",
                column: "SpecAttributeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSpecValues");
        }
    }
}
