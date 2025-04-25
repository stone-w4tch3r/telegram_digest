using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramDigest.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ConvertChannelsToFeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Verify provider before migration
            if (!migrationBuilder.IsSqlite())
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create temporary table to store Channel data
            migrationBuilder.CreateTable(
                name: "TempChannels",
                columns: table => new
                {
                    TgId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                }
            );

            // Copy data from Channels to temp table
            migrationBuilder.Sql(
                @"INSERT INTO TempChannels (TgId, Title, Description, ImageUrl, IsDeleted)
                  SELECT TgId, Title, Description, ImageUrl, IsDeleted FROM Channels"
            );

            // Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries"
            );

            migrationBuilder.DropTable(name: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_PostSummaries_ChannelTgId",
                table: "PostSummaries"
            );

            // Create new Feeds table
            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    RssUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.RssUrl);
                }
            );

            // Migrate data from temp table to Feeds
            migrationBuilder.Sql(
                @"INSERT INTO Feeds (RssUrl, Title, Description, ImageUrl, IsDeleted)
                  SELECT 'https://rsshub.app/telegram/channel/' || TgId, Title, Description, ImageUrl, IsDeleted 
                  FROM TempChannels"
            );

            // Drop temp table
            migrationBuilder.DropTable(name: "TempChannels");

            // Update PostSummaries
            migrationBuilder.AddColumn<string>(
                name: "FeedUrl",
                table: "PostSummaries",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: ""
            );

            // Migrate PostSummaries data
            migrationBuilder.Sql(
                @"UPDATE PostSummaries 
                  SET FeedUrl = 'https://rsshub.app/telegram/channel/' || ChannelTgId"
            );

            migrationBuilder.DropColumn(name: "ChannelTgId", table: "PostSummaries");

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_FeedUrl",
                table: "PostSummaries",
                column: "FeedUrl"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_IsDeleted",
                table: "Feeds",
                column: "IsDeleted"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Feeds_FeedUrl",
                table: "PostSummaries",
                column: "FeedUrl",
                principalTable: "Feeds",
                principalColumn: "RssUrl",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Verify provider before migration
            if (!migrationBuilder.IsSqlite())
            {
                throw new NotImplementedException(
                    "Migration for this database provider is not implemented."
                );
            }

            // Create temporary table to store Feed data
            migrationBuilder.CreateTable(
                name: "TempFeeds",
                columns: table => new
                {
                    RssUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                }
            );

            // Copy data from Feeds to temp table
            migrationBuilder.Sql(
                @"INSERT INTO TempFeeds (RssUrl, Title, Description, ImageUrl, IsDeleted)
                  SELECT RssUrl, Title, Description, ImageUrl, IsDeleted FROM Feeds
                  WHERE RssUrl LIKE 'https://rsshub.app/telegram/channel/%'"
            );

            // Drop foreign key and indexes
            migrationBuilder.DropForeignKey(
                name: "FK_PostSummaries_Feeds_FeedUrl",
                table: "PostSummaries"
            );

            migrationBuilder.DropTable(name: "Feeds");

            migrationBuilder.DropIndex(name: "IX_PostSummaries_FeedUrl", table: "PostSummaries");

            // Create Channels table
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    TgId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.TgId);
                }
            );

            // Migrate data back from temp table to Channels
            migrationBuilder.Sql(
                @"INSERT INTO Channels (TgId, Title, Description, ImageUrl, IsDeleted)
                  SELECT SUBSTR(RssUrl, 37), Title, Description, ImageUrl, IsDeleted 
                  FROM TempFeeds"
            );

            // Drop temp table
            migrationBuilder.DropTable(name: "TempFeeds");

            // Update PostSummaries
            migrationBuilder.AddColumn<string>(
                name: "ChannelTgId",
                table: "PostSummaries",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: ""
            );

            // Migrate PostSummaries data back
            migrationBuilder.Sql(
                @"UPDATE PostSummaries 
                  SET ChannelTgId = SUBSTR(FeedUrl, 37)
                  WHERE FeedUrl LIKE 'https://rsshub.app/telegram/channel/%'"
            );

            migrationBuilder.DropColumn(name: "FeedUrl", table: "PostSummaries");

            migrationBuilder.CreateIndex(
                name: "IX_PostSummaries_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Channels_IsDeleted",
                table: "Channels",
                column: "IsDeleted"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PostSummaries_Channels_ChannelTgId",
                table: "PostSummaries",
                column: "ChannelTgId",
                principalTable: "Channels",
                principalColumn: "TgId",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
