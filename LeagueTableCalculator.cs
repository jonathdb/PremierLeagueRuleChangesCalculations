using PremierLeagueRuleChange.Models;

public class LeagueTableCalculator
{
    public List<TeamSeasonStats> CalculateTable(List<MatchRecord> matches, string season)
    {
        var teamStats = new Dictionary<string, TeamSeasonStats>();

        // Initialize stats for all teams
        foreach (var match in matches)
        {
            if (!teamStats.ContainsKey(match.HomeTeam))
            {
                teamStats[match.HomeTeam] = new TeamSeasonStats
                {
                    Team = match.HomeTeam
                };
            }
            if (!teamStats.ContainsKey(match.AwayTeam))
            {
                teamStats[match.AwayTeam] = new TeamSeasonStats
                {
                    Team = match.AwayTeam
                };
            }
        }

        // Process each match
        foreach (var match in matches)
        {
            var homeTeam = teamStats[match.HomeTeam];
            var awayTeam = teamStats[match.AwayTeam];

            // Update goals
            homeTeam.GF += match.FTHG;
            homeTeam.GA += match.FTAG;
            awayTeam.GF += match.FTAG;
            awayTeam.GA += match.FTHG;

            // Update match counts
            homeTeam.Played++;
            awayTeam.Played++;

            // Check for 0-0 draw
            bool isZeroZeroDraw = match.FTHG == 0 && match.FTAG == 0 && match.FTR == "D";

            // Calculate points based on result
            if (match.FTR == "H") // Home win
            {
                homeTeam.Wins++;
                homeTeam.OriginalPoints += 3;
                awayTeam.Losses++;
            }
            else if (match.FTR == "A") // Away win
            {
                awayTeam.Wins++;
                awayTeam.OriginalPoints += 3;
                homeTeam.Losses++;
            }
            else if (match.FTR == "D") // Draw
            {
                homeTeam.Draws++;
                awayTeam.Draws++;
                homeTeam.OriginalPoints += 1;
                awayTeam.OriginalPoints += 1;

                if (isZeroZeroDraw)
                {
                    homeTeam.ZeroZeroDraws++;
                    awayTeam.ZeroZeroDraws++;
                }
            }
        }

        var teams = teamStats.Values.ToList();

        // Calculate original positions (sorted by original points, then goal difference, then goals scored)
        var originalSorted = teams
            .OrderByDescending(t => t.OriginalPoints)
            .ThenByDescending(t => t.GD)
            .ThenByDescending(t => t.GF)
            .ToList();

        for (int i = 0; i < originalSorted.Count; i++)
        {
            originalSorted[i].OriginalPosition = i + 1;
        }

        // Calculate new positions (sorted by new points, then goal difference, then goals scored)
        var newSorted = teams
            .OrderByDescending(t => t.NewPoints)
            .ThenByDescending(t => t.GD)
            .ThenByDescending(t => t.GF)
            .ToList();

        for (int i = 0; i < newSorted.Count; i++)
        {
            newSorted[i].NewPosition = i + 1;
        }

        return newSorted;
    }
}
