using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FootballLeague.Data;
using FootballLeague.Models;

class Program
{
    static void Main()
    {
        var options = new DbContextOptionsBuilder<FootballContext>()
                .UseSqlServer("Server=parkinson\\STEP;Database=FootballLeague;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

        using (var context = new FootballContext(options))
        {
            context.Database.EnsureCreated();

            if (!context.Teams.Any())
            {
                context.Teams.Add(new Team { Name = "Team A", City = "City A", Wins = 10, Losses = 5, Draws = 3 });
                context.Teams.Add(new Team { Name = "Team B", City = "City B", Wins = 8, Losses = 7, Draws = 3 });
                context.SaveChanges();
            }

            if (!context.Players.Any())
            {
                context.Players.Add(new Player { FullName = "John Doe", Country = "Spain", Number = 10, Position = "Forward", TeamId = 1 });
                context.SaveChanges();
            }

            if (!context.Matches.Any())
            {
                context.Matches.Add(new Match { Team1Id = 1, Team2Id = 2, GoalsTeam1 = 1, GoalsTeam2 = 1, Date = DateTime.Now });
                context.SaveChanges();
            }

            if (!context.Goals.Any())
            {
                context.Goals.Add(new Goal { PlayerId = 1, MatchId = 1 });
                context.SaveChanges();
            }

            var teams = context.Teams.ToList();
            foreach (var team in teams)
            {
                Console.WriteLine($"Team: {team.Name}, City: {team.City}, Wins: {team.Wins}, Losses: {team.Losses}, Draws: {team.Draws}");
            }
        }
    }
}