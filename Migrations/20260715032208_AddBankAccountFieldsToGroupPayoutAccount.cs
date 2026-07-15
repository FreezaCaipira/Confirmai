using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountFieldsToGroupPayoutAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "GroupPayoutAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountType",
                table: "GroupPayoutAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "GroupPayoutAccounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryCpf",
                table: "GroupPayoutAccounts",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "GroupPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BankAccountType",
                table: "GroupPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "GroupPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCpf",
                table: "GroupPayoutAccounts");
        }
    }
}
