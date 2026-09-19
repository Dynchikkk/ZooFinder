using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZooFinder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProviderSpecificConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProfiles_DisplayName",
                table: "UserProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccounts_Role",
                table: "UserAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccounts_Status",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionRooms_AnimalId",
                table: "DiscussionRooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DiscussionRooms_GeneralNotDeleted",
                table: "DiscussionRooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DiscussionRooms_Type",
                table: "DiscussionRooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DiscussionMessages_Content",
                table: "DiscussionMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Animals_LanguageCode",
                table: "Animals");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshTokenHash",
                table: "UserRefreshSessions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "UserAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "SourceItemId",
                table: "Animals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldCollation: "Latin1_General_100_BIN2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RefreshTokenHash",
                table: "UserRefreshSessions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "UserAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceItemId",
                table: "Animals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProfiles_DisplayName",
                table: "UserProfiles",
                sql: "LEN(LTRIM(RTRIM([DisplayName]))) >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccounts_Role",
                table: "UserAccounts",
                sql: "[Role] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccounts_Status",
                table: "UserAccounts",
                sql: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_AnimalId",
                table: "DiscussionRooms",
                column: "AnimalId",
                unique: true,
                filter: "[Type] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiscussionRooms_GeneralNotDeleted",
                table: "DiscussionRooms",
                sql: "[Type] <> 1 OR [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiscussionRooms_Type",
                table: "DiscussionRooms",
                sql: "[Type] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiscussionMessages_Content",
                table: "DiscussionMessages",
                sql: "LEN(LTRIM(RTRIM([Content]))) >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Animals_LanguageCode",
                table: "Animals",
                sql: "[LanguageCode] IN ('en', 'ru')");
        }
    }
}
