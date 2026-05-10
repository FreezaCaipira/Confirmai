using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddHelmetCategoryToCrownHelmet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                UPDATE ""Products""
                                SET ""Category"" = 'helmet'
                                WHERE ""Name"" ILIKE 'crown helmet'
                                    ;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                UPDATE ""Products""
                                SET ""Category"" = NULL
                                WHERE ""Name"" ILIKE 'crown helmet'
                                    AND ""Category"" = 'helmet';
            ");
        }
    }
}
