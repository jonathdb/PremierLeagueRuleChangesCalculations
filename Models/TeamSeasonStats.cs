using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PremierLeagueRuleChange.Models
{
    public class TeamSeasonStats
    {
        public string Team { get; set; } = string.Empty;
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GF { get; set; }
        public int GA { get; set; }
        public int GD => GF - GA;
        public int OriginalPoints { get; set; }
        public int ZeroZeroDraws { get; set; }
        public int NewPoints => OriginalPoints - ZeroZeroDraws;
        public int OriginalPosition { get; set; }
        public int NewPosition { get; set; }
        public int PositionChange => OriginalPosition - NewPosition;
    }
}
