namespace FootballLeague.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsAgainst { get; set; }

        public ICollection<Player> Players { get; set; }
        public ICollection<Match> MatchesAsTeam1 { get; set; }
        public ICollection<Match> MatchesAsTeam2 { get; set; }
    }
}