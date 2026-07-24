using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Souqify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Orders",
                newName: "ShippingStreet");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "Orders",
                newName: "ShippingRegion");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Orders",
                newName: "ShippingPostalCode");

            migrationBuilder.RenameColumn(
                name: "DeliveryNote",
                table: "Orders",
                newName: "ShippingDeliveryNote");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Orders",
                newName: "ShippingCity");

            migrationBuilder.RenameColumn(
                name: "Address_Street",
                table: "AspNetUsers",
                newName: "Street");

            migrationBuilder.RenameColumn(
                name: "Address_Region",
                table: "AspNetUsers",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "Address_PostalCode",
                table: "AspNetUsers",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "Address_DeliveryNote",
                table: "AspNetUsers",
                newName: "DeliveryNote");

            migrationBuilder.RenameColumn(
                name: "Address_City",
                table: "AspNetUsers",
                newName: "City");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryNote",
                table: "AspNetUsers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingStreet",
                table: "Orders",
                newName: "Street");

            migrationBuilder.RenameColumn(
                name: "ShippingRegion",
                table: "Orders",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "ShippingPostalCode",
                table: "Orders",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "ShippingDeliveryNote",
                table: "Orders",
                newName: "DeliveryNote");

            migrationBuilder.RenameColumn(
                name: "ShippingCity",
                table: "Orders",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "AspNetUsers",
                newName: "Address_Street");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "AspNetUsers",
                newName: "Address_Region");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "AspNetUsers",
                newName: "Address_PostalCode");

            migrationBuilder.RenameColumn(
                name: "DeliveryNote",
                table: "AspNetUsers",
                newName: "Address_DeliveryNote");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "AspNetUsers",
                newName: "Address_City");

            migrationBuilder.AlterColumn<string>(
                name: "Address_Street",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_Region",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_PostalCode",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_DeliveryNote",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address_City",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
