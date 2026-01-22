using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PremierLeagueRuleChange.Models
{
    public class MatchRecord
    {
        public string Date { get; set; } = string.Empty;
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public int FTHG { get; set; }   // Full Time Home Goals
        public int FTAG { get; set; }   // Full Time Away Goals
        public string FTR { get; set; } = string.Empty; // Full Time Result: H/A/D
    }
}
