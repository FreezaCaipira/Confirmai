using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Confirmai.Data;

#nullable disable

namespace Confirmai.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527190500_RequireInviteCodeForGroups")]
    public partial class RequireInviteCodeForGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Preenche códigos ausentes com valor determinístico único por Id.
                UPDATE "Groups"
                SET "InviteCode" = 'G' || LPAD("Id"::text, 7, '0')
                WHERE "InviteCode" IS NULL OR BTRIM("InviteCode") = '';

                -- Corrige colisões eventuais (incluindo casos pré-existentes raros).
                DO $$
                DECLARE
                    duplicate_code TEXT;
                    keeper_id INTEGER;
                    row_id INTEGER;
                    next_suffix INTEGER;
                    candidate TEXT;
                BEGIN
                    FOR duplicate_code IN
                        SELECT "InviteCode"
                        FROM "Groups"
                        GROUP BY "InviteCode"
                        HAVING COUNT(*) > 1
                    LOOP
                        SELECT MIN("Id") INTO keeper_id
                        FROM "Groups"
                        WHERE "InviteCode" = duplicate_code;

                        FOR row_id IN
                            SELECT "Id"
                            FROM "Groups"
                            WHERE "InviteCode" = duplicate_code
                              AND "Id" <> keeper_id
                            ORDER BY "Id"
                        LOOP
                            next_suffix := 0;
                            LOOP
                                candidate := 'G' || LPAD((row_id + 10000000 + next_suffix)::text, 7, '0');
                                EXIT WHEN NOT EXISTS (
                                    SELECT 1 FROM "Groups" WHERE "InviteCode" = candidate
                                );
                                next_suffix := next_suffix + 1;
                            END LOOP;

                            UPDATE "Groups"
                            SET "InviteCode" = candidate
                            WHERE "Id" = row_id;
                        END LOOP;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Groups_InviteCode",
                table: "Groups");

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "Groups",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_InviteCode",
                table: "Groups",
                column: "InviteCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_InviteCode",
                table: "Groups");

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "Groups",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_InviteCode",
                table: "Groups",
                column: "InviteCode",
                unique: true,
                filter: "\"InviteCode\" IS NOT NULL");
        }
    }
}
