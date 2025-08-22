using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPriceCents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Services",
                newName: "PriceCents");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceCents",
                table: "Services",
                newName: "Price");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);
        }
    }
}
