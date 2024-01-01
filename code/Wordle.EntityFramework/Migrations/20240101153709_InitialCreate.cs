using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user = table.Column<string>(type: "text", nullable: false),
                    sessionid = table.Column<Guid>(type: "uuid", nullable: false),
                    roundid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guesses", x => x.id);
                    table.ForeignKey(
                        name: "fk_session",
                        column: x => x.sessionid,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "fk_round",
                        column: x => x.roundid,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    createdat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    initialroundlength = table.Column<int>(type: "integer", nullable: false),
                    roundextensionwindow = table.Column<int>(type: "integer", nullable: false),
                    roundextensionlength = table.Column<int>(type: "integer", nullable: false),
                    maximumroundextensions = table.Column<int>(type: "integer", nullable: false),
                    minimumanswersrequired = table.Column<int>(type: "integer", nullable: false),
                    dictionaryname = table.Column<string>(type: "text", nullable: false),
                    roundvotesperuser = table.Column<int>(type: "integer", nullable: false),
                    tiebreakerstrategy = table.Column<int>(type: "integer", nullable: false),
                    numberofrounds = table.Column<int>(type: "integer", nullable: false),
                    wordlength = table.Column<int>(type: "integer", nullable: false),
                    allowguessesafterroundend = table.Column<bool>(type: "boolean", nullable: false),
                    sessionid = table.Column<string>(type: "uuid", nullable: true),
                    tenantid = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_options", x => x.id);
                    
                    table.ForeignKey(
                        name: "fk_session",
                        column: x => x.sessionid,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                });

            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    createdat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sessionid = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    guess = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rounds", x => x.id);
                    
                    table.ForeignKey(
                        name: "fk_session",
                        column: x => x.sessionid,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    createdat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant = table.Column<string>(type: "text", nullable: false),
                    word = table.Column<string>(type: "text", nullable: false),
                    usedletters = table.Column<List<string>>(type: "text[]", nullable: false),
                    activeroundid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guesses");

            migrationBuilder.DropTable(
                name: "options");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "sessions");
        }
    }
}
