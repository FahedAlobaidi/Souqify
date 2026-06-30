using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Souqify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCartEnt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cart_GuestId",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "GuestId",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "ExpireAt",
                table: "Cart",
                newName: "LastModifiedAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Cart",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Cart",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "Cart",
                newName: "ExpireAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Cart",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "GuestId",
                table: "Cart",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cart_GuestId",
                table: "Cart",
                column: "GuestId",
                unique: true);
        }
    }
}
