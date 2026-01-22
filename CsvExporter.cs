using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using PremierLeagueRuleChange.Models;
using System.Drawing;
using System.Globalization;
using System.Linq;

public class CsvExporter
{
    public void ExportResults(List<SeasonResult> results, string outputPath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, config);

        csv.WriteRecords(results);
    }

    public void ExportResultsBySeason(Dictionary<string, List<SeasonResult>> resultsBySeason, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        foreach (var kvp in resultsBySeason)
        {
            string fileName = $"results-{kvp.Key}.csv";
            string filePath = Path.Combine(outputDirectory, fileName);
            ExportResults(kvp.Value, filePath);
            Console.WriteLine($"Exported results to {filePath}");
        }
    }

    public void ExportCombinedResultsToExcel(Dictionary<string, List<SeasonResult>> resultsBySeason, Dictionary<string, List<MatchRecord>> matchesBySeason, string outputPath)
    {
        // Set EPPlus license context (required for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Ensure output directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var package = new ExcelPackage();
        
        // Sort seasons for consistent ordering
        var sortedSeasons = resultsBySeason.OrderBy(kvp => kvp.Key).ToList();

        foreach (var kvp in sortedSeasons)
        {
            var seasonCode = kvp.Key;
            var results = kvp.Value;

            // Use the formatted season name from the first result (e.g., "1993-94")
            string seasonName = results.FirstOrDefault()?.Season ?? seasonCode;

            // Create worksheet for this season
            // Excel sheet names must be <= 31 characters and cannot contain certain characters
            string sheetName = SanitizeSheetName(seasonName);
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Add note about sorting
            worksheet.Cells[1, 1].Value = $"Table sorted by New Position (after removing points from 0-0 draws)";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Italic = true;
            worksheet.Cells[1, 1].Style.Font.Size = 10;

            // Write headers (starting at row 2)
            worksheet.Cells[2, 1].Value = "Team";
            worksheet.Cells[2, 2].Value = "OriginalPosition";
            worksheet.Cells[2, 3].Value = "NewPosition";
            worksheet.Cells[2, 4].Value = "PositionChange";
            worksheet.Cells[2, 5].Value = "OriginalPoints";
            worksheet.Cells[2, 6].Value = "NewPoints";
            worksheet.Cells[2, 7].Value = "ZeroZeroDraws";
            worksheet.Cells[2, 8].Value = "GoalDifference";

            // Write data (starting at row 3, since row 1 is note, row 2 is headers)
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                int row = i + 3; // Start from row 3 (row 1 is note, row 2 is headers)

                worksheet.Cells[row, 1].Value = result.Team;
                worksheet.Cells[row, 2].Value = result.OriginalPosition;
                worksheet.Cells[row, 3].Value = result.NewPosition;
                worksheet.Cells[row, 4].Value = result.PositionChange;
                worksheet.Cells[row, 5].Value = result.OriginalPoints;
                worksheet.Cells[row, 6].Value = result.NewPoints;
                worksheet.Cells[row, 7].Value = result.ZeroZeroDraws;
                worksheet.Cells[row, 8].Value = result.GoalDifference;
            }

            // Calculate total unique 0-0 draws
            int totalZeroZeroDraws = 0;
            if (matchesBySeason.ContainsKey(seasonCode))
            {
                var matches = matchesBySeason[seasonCode];
                totalZeroZeroDraws = matches.Count(m => m.FTHG == 0 && m.FTAG == 0 && m.FTR == "D");
            }
            else
            {
                // Fallback: sum and divide by 2 (since each draw is counted twice - once per team)
                totalZeroZeroDraws = results.Sum(r => r.ZeroZeroDraws) / 2;
            }

            // Add total row
            int totalRow = results.Count + 3; // After data rows (row 1 is note, row 2 is header, rows 3+ are data)
            worksheet.Cells[totalRow, 1].Value = "Total 0-0 Draws:";
            worksheet.Cells[totalRow, 1].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 7].Value = totalZeroZeroDraws;
            worksheet.Cells[totalRow, 7].Style.Font.Bold = true;
            
            // Style the total row
            using (var totalRowRange = worksheet.Cells[totalRow, 1, totalRow, 8])
            {
                totalRowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                totalRowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 220, 220));
            }

            // Define the data range (header + data rows, excluding note and total row)
            int lastDataRow = results.Count + 2; // Last data row (row 2 is header, rows 3+ are data)
            var dataRange = worksheet.Cells[2, 1, lastDataRow, 8];

            // Create Excel table
            var table = worksheet.Tables.Add(dataRange, $"Table_{seasonCode}");
            table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
            table.ShowFilter = true; // Enable filter buttons

            // Apply row coloring AFTER table creation to ensure it persists
            // Color rows where PositionChange is not 0 (mellow red/coral)
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                int row = i + 3; // Start from row 3 (row 1 is note, row 2 is headers)

                if (result.PositionChange != 0)
                {
                    using (var rowRange = worksheet.Cells[row, 1, row, 8])
                    {
                        rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        // Mellow red/coral color (RGB: 255, 200, 200)
                        rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 200, 200));
                    }
                }
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Create matches sheet for this season
            if (matchesBySeason.ContainsKey(seasonCode))
            {
                var matches = matchesBySeason[seasonCode];
                string matchesSheetName = SanitizeSheetName($"{seasonName} - all matches");
                var matchesWorksheet = package.Workbook.Worksheets.Add(matchesSheetName);

                // Write headers
                matchesWorksheet.Cells[1, 1].Value = "Date";
                matchesWorksheet.Cells[1, 2].Value = "HomeTeam";
                matchesWorksheet.Cells[1, 3].Value = "AwayTeam";
                matchesWorksheet.Cells[1, 4].Value = "FTHG";
                matchesWorksheet.Cells[1, 5].Value = "FTAG";
                matchesWorksheet.Cells[1, 6].Value = "FTR";
                matchesWorksheet.Cells[1, 7].Value = "Score";

                // Style headers
                using (var headerRange = matchesWorksheet.Cells[1, 1, 1, 7])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(200, 200, 200));
                }

                // Write match data
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    int row = i + 2; // Start from row 2 (row 1 is headers)

                    matchesWorksheet.Cells[row, 1].Value = match.Date;
                    matchesWorksheet.Cells[row, 2].Value = match.HomeTeam;
                    matchesWorksheet.Cells[row, 3].Value = match.AwayTeam;
                    matchesWorksheet.Cells[row, 4].Value = match.FTHG;
                    matchesWorksheet.Cells[row, 5].Value = match.FTAG;
                    matchesWorksheet.Cells[row, 6].Value = match.FTR;
                    matchesWorksheet.Cells[row, 7].Value = $"{match.FTHG}-{match.FTAG}";

                    // Check if this is a 0-0 draw and highlight it
                    bool isZeroZeroDraw = match.FTHG == 0 && match.FTAG == 0 && match.FTR == "D";
                    if (isZeroZeroDraw)
                    {
                        using (var rowRange = matchesWorksheet.Cells[row, 1, row, 7])
                        {
                            rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            // Yellow highlight for 0-0 draws
                            rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 200));
                        }
                    }
                }

                // Create Excel table for matches
                int lastMatchRow = matches.Count + 1;
                var matchesDataRange = matchesWorksheet.Cells[1, 1, lastMatchRow, 7];
                var matchesTable = matchesWorksheet.Tables.Add(matchesDataRange, $"Matches_{seasonCode}");
                matchesTable.TableStyle = OfficeOpenXml.Table.TableStyles.Medium9;
                matchesTable.ShowFilter = true;

                // Auto-fit columns
                matchesWorksheet.Cells[matchesWorksheet.Dimension.Address].AutoFitColumns();
            }
        }

        // Save the Excel file
        var fileInfo = new FileInfo(outputPath);
        package.SaveAs(fileInfo);
        Console.WriteLine($"Exported combined results to {outputPath}");
    }

    private string SanitizeSheetName(string name)
    {
        // Excel sheet names have restrictions:
        // - Max 31 characters
        // - Cannot contain: \ / ? * [ ]
        // - Cannot be empty
        string sanitized = name
            .Replace("\\", "_")
            .Replace("/", "_")
            .Replace("?", "_")
            .Replace("*", "_")
            .Replace("[", "_")
            .Replace("]", "_");

        // Truncate if too long
        if (sanitized.Length > 31)
        {
            sanitized = sanitized.Substring(0, 31);
        }

        return sanitized.Length > 0 ? sanitized : "Sheet1";
    }
}
