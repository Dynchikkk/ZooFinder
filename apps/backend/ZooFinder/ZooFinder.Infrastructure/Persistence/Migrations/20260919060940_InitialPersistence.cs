using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZooFinder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InformationSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceItemId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, collation: "Latin1_General_100_BIN2"),
                    LanguageCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScientificName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSynchronizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.Id);
                    table.CheckConstraint("CK_Animals_LanguageCode", "[LanguageCode] IN ('en', 'ru')");
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, collation: "Latin1_General_100_CI_AS"),
                    PasswordHash = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.CheckConstraint("CK_UserAccounts_Role", "[Role] IN (1, 2, 3)");
                    table.CheckConstraint("CK_UserAccounts_Status", "[Status] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "DiscussionRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionRooms", x => x.Id);
                    table.CheckConstraint("CK_DiscussionRooms_GeneralNotDeleted", "[Type] <> 1 OR [IsDeleted] = 0");
                    table.CheckConstraint("CK_DiscussionRooms_Type", "[Type] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_DiscussionRooms_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.CheckConstraint("CK_UserProfiles_DisplayName", "LEN(LTRIM(RTRIM([DisplayName]))) >= 1");
                    table.ForeignKey(
                        name: "FK_UserProfiles_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshSessions_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiscussionMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscussionRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionMessages", x => x.Id);
                    table.CheckConstraint("CK_DiscussionMessages_Content", "LEN(LTRIM(RTRIM([Content]))) >= 1");
                    table.ForeignKey(
                        name: "FK_DiscussionMessages_DiscussionRooms_DiscussionRoomId",
                        column: x => x.DiscussionRoomId,
                        principalTable: "DiscussionRooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionMessages_UserAccounts_AuthorUserAccountId",
                        column: x => x.AuthorUserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_InformationSource_LanguageCode_SourceItemId",
                table: "Animals",
                columns: new[] { "InformationSource", "LanguageCode", "SourceItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Animals_LanguageCode_CreatedAtUtc_Id",
                table: "Animals",
                columns: new[] { "LanguageCode", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionMessages_AuthorUserAccountId",
                table: "DiscussionMessages",
                column: "AuthorUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionMessages_DiscussionRoomId_CreatedAtUtc_Id",
                table: "DiscussionMessages",
                columns: new[] { "DiscussionRoomId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_AnimalId",
                table: "DiscussionRooms",
                column: "AnimalId",
                unique: true,
                filter: "[Type] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_AnimalId_Name",
                table: "DiscussionRooms",
                columns: new[] { "AnimalId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Login",
                table: "UserAccounts",
                column: "Login",
                unique: true,
                filter: "[Login] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserAccountId",
                table: "UserProfiles",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshSessions_RefreshTokenHash",
                table: "UserRefreshSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshSessions_UserAccountId",
                table: "UserRefreshSessions",
                column: "UserAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscussionMessages");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserRefreshSessions");

            migrationBuilder.DropTable(
                name: "DiscussionRooms");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Animals");
        }
    }
}
