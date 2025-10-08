using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGames.Games.Infra.Migrations
{
    /// <inheritdoc />
    public partial class EnsureStoredEventsAndDropGameEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[StoredEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StoredEvents](
        [Id] [uniqueidentifier] NOT NULL,
        [Type] [nvarchar](200) NOT NULL,
        [Payload] [nvarchar](max) NOT NULL,
        [OccurredAt] [datetime2] NOT NULL,
        CONSTRAINT [PK_StoredEvents] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
            ");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameEvents]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[GameEvents];
END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GameEvents](
        [Id] [uniqueidentifier] NOT NULL,
        [Type] [nvarchar](50) NOT NULL,
        [Payload] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime2] NOT NULL,
        CONSTRAINT [PK_GameEvents] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
            ");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[StoredEvents]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[StoredEvents];
END
            ");
        }
    }
}


