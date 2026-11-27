using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dumbo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CrearAuditoriaCuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_audit", x => x.id);
                    table.CheckConstraint("ck_account_audit_action", "action IN (1, 2)");
                    table.CheckConstraint("ck_account_audit_new_status", "new_status IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_account_audit_previous_status", "previous_status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_account_audit_actor",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_audit_target",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_audit_actor_user_id",
                table: "account_audit",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_audit_target",
                table: "account_audit",
                columns: new[] { "target_user_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_audit");
        }
    }
}
