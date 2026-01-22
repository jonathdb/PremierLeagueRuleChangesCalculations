using System.Net.Http;

public class CsvDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://datahub.io/core/english-premier-league/_r/-/";
    private readonly string _cacheDirectory = "cache";

    public CsvDownloadService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        
        // Create cache directory if it doesn't exist
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<string> DownloadSeasonAsync(string seasonCode)
    {
        string fileName = $"season-{seasonCode}.csv";
        string cachePath = Path.Combine(_cacheDirectory, fileName);
        string url = $"{_baseUrl}{fileName}";

        // Return cached file if it exists
        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        try
        {
            Console.WriteLine($"Downloading {fileName}...");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(cachePath, content);
            
            Console.WriteLine($"Downloaded and cached {fileName}");
            return cachePath;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to download {fileName} from {url}: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> DownloadSeasonsAsync(List<string> seasonCodes)
    {
        var downloadedFiles = new List<string>();
        
        foreach (var seasonCode in seasonCodes)
        {
            try
            {
                var filePath = await DownloadSeasonAsync(seasonCode);
                downloadedFiles.Add(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not download season {seasonCode}: {ex.Message}");
            }
        }

        return downloadedFiles;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
