using Microsoft.EntityFrameworkCore.Migrations;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Games",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Developer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Publisher = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                Genre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Games", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "GameEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameEvents", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "StoredEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_StoredEvents", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_OutboxMessages", x => x.Id); });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OutboxMessages");
        migrationBuilder.DropTable(name: "StoredEvents");
        migrationBuilder.DropTable(name: "GameEvents");
        migrationBuilder.DropTable(name: "Games");
    }
}


