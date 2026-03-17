using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupportTickets')
                BEGIN
                    CREATE TABLE [SupportTickets] (
                        [Id] uniqueidentifier NOT NULL,
                        [CustomerId] uniqueidentifier NOT NULL,
                        [CustomerName] nvarchar(max) NOT NULL,
                        [CustomerEmail] nvarchar(max) NOT NULL,
                        [Subject] nvarchar(max) NOT NULL,
                        [Category] nvarchar(max) NOT NULL,
                        [Status] nvarchar(max) NOT NULL,
                        [Priority] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [ResolvedAt] datetime2 NULL,
                        CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id])
                    )
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupportMessages')
                BEGIN
                    CREATE TABLE [SupportMessages] (
                        [Id] uniqueidentifier NOT NULL,
                        [TicketId] uniqueidentifier NOT NULL,
                        [SenderId] uniqueidentifier NOT NULL,
                        [SenderName] nvarchar(max) NOT NULL,
                        [SenderRole] nvarchar(max) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [IsStaff] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_SupportMessages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SupportMessages_SupportTickets_TicketId]
                            FOREIGN KEY ([TicketId]) REFERENCES [SupportTickets]([Id]) ON DELETE CASCADE
                    )
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupportMessages_TicketId')
                BEGIN
                    CREATE INDEX [IX_SupportMessages_TicketId] ON [SupportMessages]([TicketId])
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SupportMessages");
            migrationBuilder.DropTable(name: "SupportTickets");
        }
    }
}
