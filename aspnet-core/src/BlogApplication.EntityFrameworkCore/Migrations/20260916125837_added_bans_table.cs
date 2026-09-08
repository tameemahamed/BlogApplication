using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApplication.Migrations
{
    /// <inheritdoc />
    public partial class added_bans_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BanType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BannedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    LiftedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LiftedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBans_AbpUsers_BannedByUserId",
                        column: x => x.BannedByUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_AbpUsers_LiftedByUserId",
                        column: x => x.LiftedByUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_BannedByUserId",
                table: "UserBans",
                column: "BannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_LiftedByUserId",
                table: "UserBans",
                column: "LiftedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserBans_ActiveType",
                table: "UserBans",
                columns: new[] { "UserId", "BanType" },
                unique: true,
                filter: "\"LiftedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBans");
        }
    }
}
