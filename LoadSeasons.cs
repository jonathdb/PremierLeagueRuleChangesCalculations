using CsvHelper;
using CsvHelper.Configuration;
using PremierLeagueRuleChange.Models;
using System.Globalization;

public class CsvLoader
{
    public static List<MatchRecord> LoadSeason(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"CSV file not found: {path}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<MatchRecordMap>();
        return csv.GetRecords<MatchRecord>().ToList();
    }

    public sealed class MatchRecordMap : ClassMap<MatchRecord>
    {
        public MatchRecordMap()
        {
            Map(m => m.Date).Name("Date");
            Map(m => m.HomeTeam).Name("HomeTeam");
            Map(m => m.AwayTeam).Name("AwayTeam");
            Map(m => m.FTHG).Name("FTHG");
            Map(m => m.FTAG).Name("FTAG");
            Map(m => m.FTR).Name("FTR");
        }
    }
}
