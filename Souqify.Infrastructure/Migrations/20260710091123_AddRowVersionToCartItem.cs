using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Souqify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId_ProductId_ProductVariantId",
                table: "CartItem");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CartItem",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId_ProductVariantId",
                table: "CartItem",
                columns: new[] { "CartId", "ProductVariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId_ProductVariantId",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CartItem");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId_ProductId_ProductVariantId",
                table: "CartItem",
                columns: new[] { "CartId", "ProductId", "ProductVariantId" },
                unique: true);
        }
    }
}
