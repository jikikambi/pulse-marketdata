using System.Reflection;
using System.Text.Json;

namespace MarketData.Adapter.Shared.Util;

public static class DataHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<T?> GetDataAsDeserializedObjectAsync<T>(string folder, string fileName) where T : class
    {
        var jsonString = GetDataFromDiskStream(folder, fileName);

        return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions); 
    }

    private static string GetDataFromDiskStream(string folder, string fileName)
    {
        var binFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var filePath = Path.Combine(binFolderPath!, folder, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File {filePath} not found");

        return File.ReadAllText(filePath);
    }
}
