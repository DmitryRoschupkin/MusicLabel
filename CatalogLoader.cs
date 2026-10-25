using System.Text.Json;

namespace MusicLabel;

public static class CatalogLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static Result<ReleaseCatalog> Load(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result<ReleaseCatalog>.Fail(Errors.FileNotFound(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<ReleaseCatalog>.Fail(Errors.FileUnreadable(path, ex.Message));
        }
        
        return ParseEntries(path, json).Bind(ReleaseCatalog.From);
    }

    private static Result<List<CatalogEntry>> ParseEntries(string path, string json)
    {
        try
        {
            var entries = JsonSerializer.Deserialize<List<CatalogEntry>>(json, Options);
            return entries is null
                ? Result<List<CatalogEntry>>.Fail(Errors.MalformedFile(path, "the document is 'null'."))
                : Result<List<CatalogEntry>>.Ok(entries);
        }
        catch (JsonException ex)
        {
            return Result<List<CatalogEntry>>.Fail(Errors.MalformedFile(path, ex.Message));
        }
    }
}