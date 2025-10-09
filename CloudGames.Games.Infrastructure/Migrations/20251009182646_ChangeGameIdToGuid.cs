using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGames.Games.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGameIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Dropar a foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Games_GameId",
                table: "Promotions");

            // 2. Limpar dados existentes (conversão de string para Guid não é possível)
            migrationBuilder.Sql("DELETE FROM [Promotions]");
            migrationBuilder.Sql("DELETE FROM [OutboxMessages]");
            migrationBuilder.Sql("DELETE FROM [StoredEvents]");
            migrationBuilder.Sql("DELETE FROM [Games]");

            // 3. Dropar a primary key de Games
            migrationBuilder.DropPrimaryKey(
                name: "PK_Games",
                table: "Games");

            // 4. Dropar a primary key de Promotions
            migrationBuilder.DropPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions");

            // 5. Alterar o tipo das colunas
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Games",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "GameId",
                table: "Promotions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Promotions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // 6. Recriar as primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Games",
                table: "Games",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions",
                column: "Id");

            // 7. Recriar a foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Games_GameId",
                table: "Promotions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Dropar a foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Games_GameId",
                table: "Promotions");

            // 2. Dropar as primary keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_Games",
                table: "Games");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions");

            // 3. Reverter o tipo das colunas
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Games",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "GameId",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Promotions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // 4. Recriar as primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Games",
                table: "Games",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions",
                column: "Id");

            // 5. Recriar a foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Games_GameId",
                table: "Promotions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
