using Microsoft.EntityFrameworkCore;
using FootballLeague.Models;
using Microsoft.EntityFrameworkCore.Design;

namespace FootballLeague.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FootballContext>
    {
        public FootballContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FootballContext>();
            optionsBuilder.UseSqlServer("Server=parkinson\\STEP;Database=FootballLeague;Trusted_Connection=True;TrustServerCertificate=True;"); // Замените на вашу строку подключения

            return new FootballContext(optionsBuilder.Options);
        }
    }

    public class FootballContext : DbContext
    {
        public FootballContext(DbContextOptions<FootballContext> options) : base(options) { }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Goal> Goals { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=parkinson\\STEP;Database=FootballLeague;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>()
                .HasMany(t => t.Players)
                .WithOne(p => p.Team)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.MatchesAsTeam1)
                .WithOne(m => m.Team1)
                .HasForeignKey(m => m.Team1Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.MatchesAsTeam2)
                .WithOne(m => m.Team2)
                .HasForeignKey(m => m.Team2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasMany(m => m.Goals)
                .WithOne(g => g.Match)
                .HasForeignKey(g => g.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.Goals)
                .WithOne(g => g.Player)
                .HasForeignKey(g => g.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public async Task FillRandomMatches()
        {
            await Database.ExecuteSqlRawAsync("EXEC FillRandomMatches");
        }

        public async Task<Team> GetTeamByNameAsync(string name)
        {
            return await Teams.FirstOrDefaultAsync(t => t.Name == name);
        }
        public async Task<List<Team>> GetTeamsByCityAsync(string city)
        {
            return await Teams.Where(t => t.City == city).ToListAsync();
        }
        public async Task<Team> GetTeamByNameAndCityAsync(string name, string city)
        {
            return await Teams.FirstOrDefaultAsync(t => t.Name == name && t.City == city);
        }
        public async Task<Team> GetTeamWithMostWinsAsync()
        {
            return await Teams.OrderByDescending(t => t.Wins).FirstOrDefaultAsync();
        }
        public async Task<Team> GetTeamWithMostLossesAsync()
        {
            return await Teams.OrderByDescending(t => t.Losses).FirstOrDefaultAsync();
        }
        public async Task<Team> GetTeamWithMostDrawsAsync()
        {
            return await Teams.OrderByDescending(t => t.Draws).FirstOrDefaultAsync();
        }
        public async Task<Team> GetTeamWithMostGoalsScoredAsync()
        {
            return await Teams.OrderByDescending(t => t.GoalsScored).FirstOrDefaultAsync();
        }
        public async Task<Team> GetTeamWithMostGoalsAgainstAsync()
        {
            return await Teams.OrderByDescending(t => t.GoalsAgainst).FirstOrDefaultAsync();
        }
        public async Task AddTeamAsync(Team newTeam)
        {
            var existingTeam = await GetTeamByNameAndCityAsync(newTeam.Name, newTeam.City);
            if (existingTeam != null)
            {
                throw new InvalidOperationException("Team already exists.");
            }
            Teams.Add(newTeam);
            await SaveChangesAsync();
        }
        public async Task UpdateTeamAsync(int teamId, string newName, string newCity, int newWins, int newLosses, int newDraws, int newGoalsScored, int newGoalsConceded)
        {
            var team = await Teams.FindAsync(teamId);
            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            team.Name = newName;
            team.City = newCity;
            team.Wins = newWins;
            team.Losses = newLosses;
            team.Draws = newDraws;
            team.GoalsScored = newGoalsScored;
            team.GoalsAgainst = newGoalsConceded;

            await SaveChangesAsync();
        }
        public async Task DeleteTeamAsync(string name, string city)
        {
            var team = await GetTeamByNameAndCityAsync(name, city);
            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            Console.WriteLine($"Are you sure you want to delete the team '{name}' from '{city}'? (y/n)");
            var confirmation = Console.ReadLine();
            if (confirmation?.ToLower() == "y")
            {
                Teams.Remove(team);
                await SaveChangesAsync();
                Console.WriteLine("Team deleted successfully.");
            }
            else
            {
                Console.WriteLine("Deletion canceled.");
            }
        }
        public async Task<List<(int TeamId, string TeamName, int GoalDifference)>> GetTeamsGoalDifferenceAsync()
        {
            var result = await Teams
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    GoalDifference = t.GoalsScored - t.GoalsAgainst
                })
                .ToListAsync();

            return result.Select(r => (r.Id, r.Name, r.GoalDifference)).ToList();
        }
        public async Task<(int MatchId, string Team1, string Team2, int GoalsTeam1, int GoalsTeam2, DateTime Date, List<(string PlayerName, DateTime GoalDate)> Goals)> GetMatchDetailsAsync(int matchId)
        {
            var match = await Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Include(m => m.Goals)
                .ThenInclude(g => g.Player)
                .Where(m => m.Id == matchId)
                .Select(m => new
                {
                    m.Id,
                    Team1 = m.Team1.Name,
                    Team2 = m.Team2.Name,
                    m.GoalsTeam1,
                    m.GoalsTeam2,
                    m.Date,
                    Goals = m.Goals.Select(g => new
                    {
                        g.Player.FullName,
                        m.Date
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (match == null) return default;

            return (match.Id, match.Team1, match.Team2, match.GoalsTeam1, match.GoalsTeam2, match.Date, match.Goals.Select(g => (g.FullName, g.Date)).ToList());
        }
        public async Task<List<(int MatchId, string Team1, string Team2, int GoalsTeam1, int GoalsTeam2)>> GetMatchesByDateAsync(DateTime date)
        {
            var result = await Matches
                .Where(m => m.Date.Date == date.Date)
                .Select(m => new
                {
                    m.Id,
                    Team1 = m.Team1.Name,
                    Team2 = m.Team2.Name,
                    m.GoalsTeam1,
                    m.GoalsTeam2
                })
                .ToListAsync();

            return result.Select(r => (r.Id, r.Team1, r.Team2, r.GoalsTeam1, r.GoalsTeam2)).ToList();
        }
        public async Task<List<(int MatchId, string Team1, string Team2, int GoalsTeam1, int GoalsTeam2)>> GetMatchesByTeamAsync(int teamId)
        {
            var result = await Matches
                .Where(m => m.Team1Id == teamId || m.Team2Id == teamId)
                .Select(m => new
                {
                    m.Id,
                    Team1 = m.Team1.Name,
                    Team2 = m.Team2.Name,
                    m.GoalsTeam1,
                    m.GoalsTeam2
                })
                .ToListAsync();

            return result.Select(r => (r.Id, r.Team1, r.Team2, r.GoalsTeam1, r.GoalsTeam2)).ToList();
        }
        public async Task<List<(string PlayerName, int MatchId)>> GetPlayersWithGoalsByDateAsync(DateTime date)
        {
            var result = await Goals
                .Include(g => g.Match)
                .Include(g => g.Player)
                .Where(g => g.Match.Date.Date == date.Date)
                .Select(g => new
                {
                    PlayerName = g.Player.FullName,
                    g.MatchId
                })
                .ToListAsync();

            return result.Select(r => (r.PlayerName, r.MatchId)).ToList();
        }
        public async Task AddMatchAsync(Match newMatch)
        {
            var existingMatch = await Matches
                .AnyAsync(m => (m.Team1Id == newMatch.Team1Id && m.Team2Id == newMatch.Team2Id && m.Date == newMatch.Date));

            if (existingMatch)
            {
                throw new InvalidOperationException("Match already exists.");
            }

            Matches.Add(newMatch);
            await SaveChangesAsync();
        }
        public async Task UpdateMatchAsync(int matchId, int team1Id, int team2Id, int goalsTeam1, int goalsTeam2, DateTime date)
        {
            var match = await Matches.FindAsync(matchId);
            if (match == null) throw new InvalidOperationException("Match not found.");

            match.Team1Id = team1Id;
            match.Team2Id = team2Id;
            match.GoalsTeam1 = goalsTeam1;
            match.GoalsTeam2 = goalsTeam2;
            match.Date = date;

            await SaveChangesAsync();
        }
        public async Task DeleteMatchAsync(int team1Id, int team2Id, DateTime date)
        {
            var match = await Matches
                .FirstOrDefaultAsync(m => m.Team1Id == team1Id && m.Team2Id == team2Id && m.Date == date);

            if (match == null) throw new InvalidOperationException("Match not found.");

            Console.WriteLine($"Are you sure you want to delete the match between team {team1Id} and team {team2Id} on {date}? (y/n)");
            var confirmation = Console.ReadLine();
            if (confirmation?.ToLower() == "y")
            {
                Matches.Remove(match);
                await SaveChangesAsync();
                Console.WriteLine("Match deleted successfully.");
            }
            else
            {
                Console.WriteLine("Deletion canceled.");
            }
        }
        public async Task<List<Player>> GetTopScorersForTeamAsync(int teamId, int topCount)
        {
            return await Goals
                .Where(g => g.Player.TeamId == teamId)
                .GroupBy(g => g.Player)
                .Select(g => new
                {
                    Player = g.Key,
                    GoalCount = g.Count()
                })
                .OrderByDescending(p => p.GoalCount)
                .Take(topCount)
                .Select(p => p.Player)
                .ToListAsync();
        }

        public async Task<Player> GetTopScorerForTeamAsync(int teamId)
        {
            var topScorers = await GetTopScorersForTeamAsync(teamId, 1);
            return topScorers.FirstOrDefault();
        }

        public async Task<List<Player>> GetTopScorersOverallAsync(int topCount)
        {
            return await Goals
                .GroupBy(g => g.Player)
                .Select(g => new
                {
                    Player = g.Key,
                    GoalCount = g.Count()
                })
                .OrderByDescending(p => p.GoalCount)
                .Take(topCount)
                .Select(p => p.Player)
                .ToListAsync();
        }

        public async Task<Player> GetTopScorerOverallAsync()
        {
            var topScorers = await GetTopScorersOverallAsync(1);
            return topScorers.FirstOrDefault();
        }

        public async Task<List<Team>> GetTopScoringTeamsAsync(int topCount)
        {
            return await Teams
                .OrderByDescending(t => t.GoalsScored)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<Team> GetTopScoringTeamAsync()
        {
            return await Teams
                .OrderByDescending(t => t.GoalsScored)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Team>> GetTeamsWithFewestGoalsAgainstAsync(int topCount)
        {
            return await Teams
                .OrderBy(t => t.GoalsAgainst)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<Team> GetTeamWithFewestGoalsAgainstAsync()
        {
            return await Teams
                .OrderBy(t => t.GoalsAgainst)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Team>> GetTopTeamsByPointsAsync(int topCount)
        {
            return await Teams
                .OrderByDescending(t => (t.Wins * 3) + (t.Draws * 1))
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<Team> GetTopTeamByPointsAsync()
        {
            return await Teams
                .OrderByDescending(t => (t.Wins * 3) + (t.Draws * 1))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Team>> GetBottomTeamsByPointsAsync(int bottomCount)
        {
            return await Teams
                .OrderBy(t => (t.Wins * 3) + (t.Draws * 1))
                .Take(bottomCount)
                .ToListAsync();
        }

        public async Task<Team> GetBottomTeamByPointsAsync()
        {
            return await Teams
                .OrderBy(t => (t.Wins * 3) + (t.Draws * 1))
                .FirstOrDefaultAsync();
        }
    }
}