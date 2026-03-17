using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive'
                )
                BEGIN
                    ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT 1
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive'
                )
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [IsActive]
                END
            ");
        }
    }
}
