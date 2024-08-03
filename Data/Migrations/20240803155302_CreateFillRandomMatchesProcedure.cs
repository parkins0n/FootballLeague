using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateFillRandomMatchesProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE PROCEDURE FillRandomMatches
            AS
            BEGIN
                DECLARE @TeamCount INT;
                SELECT @TeamCount = COUNT(*) FROM Teams;

                INSERT INTO Matches (Team1Id, Team2Id, GoalsTeam1, GoalsTeam2, Date)
                SELECT 
                    t1.Id, 
                    t2.Id,
                    ABS(CHECKSUM(NEWID()) % 5),
                    ABS(CHECKSUM(NEWID()) % 5),
                    DATEADD(DAY, ABS(CHECKSUM(NEWID()) % 30), GETDATE())
                FROM 
                    (SELECT TOP 10 * FROM Teams) t1
                CROSS JOIN 
                    (SELECT TOP 10 * FROM Teams) t2
                WHERE 
                    t1.Id <> t2.Id;
            END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS FillRandomMatches");
        }
    }
}
