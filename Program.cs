using PremierLeagueRuleChange.Models;

// Generate all season codes from 1993-94 (9394) to 2023-24 (2324)
static List<string> GetAllSeasonCodes()
{
    var seasons = new List<string>();
    for (int year = 1993; year <= 2023; year++)
    {
        int nextYear = year + 1;
        string seasonCode = $"{year % 100:D2}{nextYear % 100:D2}";
        seasons.Add(seasonCode);
    }
    return seasons;
}

// Parse season codes from command line arguments
static List<string> ParseSeasonCodes(string[] args)
{
    var seasonCodes = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--seasons" && i + 1 < args.Length)
        {
            var codes = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            seasonCodes.AddRange(codes);
            i++; // Skip the next argument as we've processed it
        }
        else if (args[i] == "--all")
        {
            return GetAllSeasonCodes();
        }
    }

    // If no seasons specified, default to all
    if (seasonCodes.Count == 0)
    {
        Console.WriteLine("No seasons specified. Use --seasons <codes> or --all. Defaulting to all seasons.");
        return GetAllSeasonCodes();
    }

    return seasonCodes;
}

// Convert season code to readable format (e.g., "9394" -> "1993-94")
static string FormatSeason(string seasonCode)
{
    if (seasonCode.Length != 4)
        return seasonCode;

    int year1 = int.Parse(seasonCode.Substring(0, 2));
    int year2 = int.Parse(seasonCode.Substring(2, 2));

    // Determine century
    int fullYear1 = year1 < 50 ? 2000 + year1 : 1900 + year1;
    int fullYear2 = year2 < 50 ? 2000 + year2 : 1900 + year2;

    return $"{fullYear1}-{fullYear2 % 100:D2}";
}

// Convert TeamSeasonStats to SeasonResult
static SeasonResult ConvertToSeasonResult(TeamSeasonStats stats, string season)
{
    return new SeasonResult
    {
        Season = season,
        Team = stats.Team,
        OriginalPosition = stats.OriginalPosition,
        NewPosition = stats.NewPosition,
        PositionChange = stats.PositionChange,
        OriginalPoints = stats.OriginalPoints,
        NewPoints = stats.NewPoints,
        ZeroZeroDraws = stats.ZeroZeroDraws,
        GoalDifference = stats.GD
    };
}

Console.WriteLine("Premier League Rule Change Calculator");
Console.WriteLine("=====================================");
Console.WriteLine();

CsvDownloadService? downloadService = null;
try
{
    // Parse command-line arguments
    var seasonCodes = ParseSeasonCodes(args);
    Console.WriteLine($"Processing {seasonCodes.Count} season(s)...");
    Console.WriteLine();

    downloadService = new CsvDownloadService();
    var calculator = new LeagueTableCalculator();
    var exporter = new CsvExporter();

    var allResults = new List<SeasonResult>();
    var resultsBySeason = new Dictionary<string, List<SeasonResult>>();
    var matchesBySeason = new Dictionary<string, List<MatchRecord>>();

    // Process each season
    foreach (var seasonCode in seasonCodes)
    {
        try
        {
            string formattedSeason = FormatSeason(seasonCode);
            Console.WriteLine($"Processing season {formattedSeason} ({seasonCode})...");

            // Download CSV
            string csvPath = await downloadService.DownloadSeasonAsync(seasonCode);

            // Load matches
            var matches = CsvLoader.LoadSeason(csvPath);
            Console.WriteLine($"  Loaded {matches.Count} matches");

            // Store matches for this season
            matchesBySeason[seasonCode] = matches;

            // Calculate table
            var teamStats = calculator.CalculateTable(matches, formattedSeason);
            Console.WriteLine($"  Calculated table for {teamStats.Count} teams");

            // Convert to results
            var seasonResults = teamStats.Select(s => ConvertToSeasonResult(s, formattedSeason)).ToList();
            allResults.AddRange(seasonResults);
            resultsBySeason[seasonCode] = seasonResults;

            Console.WriteLine($"  Completed {formattedSeason}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error processing season {seasonCode}: {ex.Message}");
            Console.WriteLine();
        }
    }

    // Export results
    string outputDir = "output";
    exporter.ExportCombinedResultsToExcel(resultsBySeason, matchesBySeason, Path.Combine(outputDir, "all-seasons-results.xlsx"));
    exporter.ExportResultsBySeason(resultsBySeason, outputDir);

    Console.WriteLine("=====================================");
    Console.WriteLine("Processing complete!");
    Console.WriteLine($"Results exported to {outputDir} directory");
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
finally
{
    // Cleanup
    downloadService?.Dispose();
}
