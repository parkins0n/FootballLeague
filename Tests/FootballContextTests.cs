using Microsoft.EntityFrameworkCore;
using Xunit;
using FootballLeague.Data;
using FootballLeague.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FootballLeague.Tests
{
    public class FootballContextTests : IDisposable
    {
        private readonly FootballContext _context;

        public FootballContextTests()
        {
            var options = new DbContextOptionsBuilder<FootballContext>()
                .UseInMemoryDatabase("FootballTestDb")
                .Options;

            _context = new FootballContext(options);
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            if (!_context.Teams.Any())
            {
                var team1 = new Team
                {
                    Id = 1,
                    Name = "Team A",
                    City = "City A",
                    Wins = 10,
                    Losses = 5,
                    Draws = 3,
                    GoalsScored = 25,
                    GoalsAgainst = 15
                };
                var team2 = new Team
                {
                    Id = 2,
                    Name = "Team B",
                    City = "City B",
                    Wins = 8,
                    Losses = 7,
                    Draws = 3,
                    GoalsScored = 20,
                    GoalsAgainst = 18
                };

                _context.Teams.AddRange(team1, team2);
                _context.SaveChanges();
            }

            if (!_context.Matches.Any())
            {
                var match1 = new Match
                {
                    Id = 1,
                    Team1Id = 1,
                    Team2Id = 2,
                    GoalsTeam1 = 2,
                    GoalsTeam2 = 1,
                    Date = DateTime.Now
                };
                _context.Matches.Add(match1);
                _context.SaveChanges();
            }

            if (!_context.Players.Any())
            {
                _context.Players.AddRange(new[]
                {
                    new Player
                    {
                        FullName = "Player One",
                        Country = "Country A",
                        Number = 10,
                        Position = "Forward"
                    },
                    new Player
                    {
                        FullName = "Player Two",
                        Country = "Country B",
                        Number = 11,
                        Position = "Midfielder"
                    }
                });

                _context.SaveChanges();
            }
        }

        [Fact]
        public async Task CanAddAndRetrieveTeam()
        {
            var team = new Team
            {
                Name = "Team C",
                City = "City C",
                Wins = 0,
                Losses = 0,
                Draws = 0,
                GoalsScored = 0,
                GoalsAgainst = 0
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var retrievedTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == "Team C" && t.City == "City C");

            Assert.NotNull(retrievedTeam);
            Assert.Equal("Team C", retrievedTeam.Name);
            Assert.Equal("City C", retrievedTeam.City);
        }

        [Fact]
        public async Task CanUpdateTeam()
        {
            // Arrange
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == "Team A" && t.City == "City A");

            if (team == null)
            {
                Assert.True(false, "Initial team not found for update.");
                return;
            }

            team.Name = "Updated Team A";
            team.City = "Updated City A";
            team.Wins = 11;
            team.Losses = 4;
            team.Draws = 5;
            team.GoalsScored = 30;
            team.GoalsAgainst = 10;

            _context.Teams.Update(team);
            await _context.SaveChangesAsync();

            var updatedTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == "Updated Team A" && t.City == "Updated City A");

            Assert.NotNull(updatedTeam);
            Assert.Equal(11, updatedTeam.Wins);
            Assert.Equal(4, updatedTeam.Losses);
        }

        [Fact]
        public async Task CanDeleteTeam()
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == "Team A" && t.City == "City A");

            if (team == null)
            {
                Assert.True(false, "Initial team not found for deletion.");
                return;
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            var deletedTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == "Team A" && t.City == "City A");

            Assert.Null(deletedTeam);
        }

        [Fact]
        public async Task CanRetrieveTopScoringTeams()
        {
            var topTeams = await _context.Teams
                .OrderByDescending(t => t.GoalsScored)
                .Take(1)
                .ToListAsync();

            Assert.Single(topTeams);
            Assert.Equal("Team A", topTeams.First().Name);
        }

        [Fact]
        public async Task CanRetrieveTopScorersOverall()
        {
            var topScorers = await _context.Players
                .OrderByDescending(p => p.Number)
                .Take(1)
                .ToListAsync();

            Assert.Single(topScorers);
            Assert.Equal("Player Two", topScorers.First().FullName);
        }

        [Fact]
        public async Task CanRetrieveMatchDetails()
        {
            var matchDetails = await _context.Matches
                .Where(m => m.Id == 1)
                .Select(m => new
                {
                    m.Id,
                    Team1 = _context.Teams.FirstOrDefault(t => t.Id == m.Team1Id).Name,
                    Team2 = _context.Teams.FirstOrDefault(t => t.Id == m.Team2Id).Name,
                    m.GoalsTeam1,
                    m.GoalsTeam2,
                    m.Date
                })
                .FirstOrDefaultAsync();

            Assert.NotNull(matchDetails);
            Assert.Equal(1, matchDetails.Id);
            Assert.Equal("Team A", matchDetails.Team1);
            Assert.Equal("Team B", matchDetails.Team2);
            Assert.Equal(2, matchDetails.GoalsTeam1);
            Assert.Equal(1, matchDetails.GoalsTeam2);
        }

        [Fact]
        public async Task CanAddAndRetrieveMatch()
        {
            var match = new Match
            {
                Team1Id = 1,
                Team2Id = 2,
                GoalsTeam1 = 1,
                GoalsTeam2 = 1,
                Date = DateTime.Now
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            var retrievedMatch = await _context.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .FirstOrDefaultAsync(m => m.Team1Id == 1 && m.Team2Id == 2 && m.Date == match.Date);

            Assert.NotNull(retrievedMatch);
            Assert.Equal(1, retrievedMatch.GoalsTeam1);
            Assert.Equal(1, retrievedMatch.GoalsTeam2);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}