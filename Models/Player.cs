namespace FootballLeague.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Country { get; set; }
        public int Number { get; set; }
        public string Position { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }

        public ICollection<Goal> Goals { get; set; }
    }
}