namespace PremierLeagueRuleChange.Models
{
    public class SeasonResult
    {
        public string Season { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int OriginalPosition { get; set; }
        public int NewPosition { get; set; }
        public int PositionChange { get; set; }
        public int OriginalPoints { get; set; }
        public int NewPoints { get; set; }
        public int ZeroZeroDraws { get; set; }
        public int GoalDifference { get; set; }
    }
}
