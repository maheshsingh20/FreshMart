using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DiscountPercent column if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'DiscountPercent'
                )
                BEGIN
                    ALTER TABLE [Products] ADD [DiscountPercent] decimal(5,2) NOT NULL DEFAULT 0.00
                END
            ");

            // Add FK on SupportTickets.CustomerId if missing
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_SupportTickets_Users_CustomerId'
                )
                BEGIN
                    ALTER TABLE [SupportTickets]
                    ADD CONSTRAINT [FK_SupportTickets_Users_CustomerId]
                    FOREIGN KEY ([CustomerId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                END
            ");

            // Add index on SupportTickets.CustomerId if missing
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupportTickets_CustomerId')
                BEGIN
                    CREATE INDEX [IX_SupportTickets_CustomerId] ON [SupportTickets]([CustomerId])
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'DiscountPercent'
                )
                BEGIN
                    ALTER TABLE [Products] DROP COLUMN [DiscountPercent]
                END
            ");
        }
    }
}
