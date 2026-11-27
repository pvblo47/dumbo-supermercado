using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dumbo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVersionSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "session_version",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "session_version",
                table: "users");
        }
    }
}
