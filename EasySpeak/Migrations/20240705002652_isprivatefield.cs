using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasySpeak.Migrations
{
    /// <inheritdoc />
    public partial class isprivatefield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "ChatRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "ChatRooms");
        }
    }
}
