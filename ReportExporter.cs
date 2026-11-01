using System.Text;
namespace MusicLabel;

public static class ReportExporter
{
    public static Result<int> Write(Func<Stream> openOutput, IEnumerable<string> lines, string target)
    {
        try
        {
            using var stream = openOutput();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            int count = 0;
            foreach (var line in lines)
            {
                writer.WriteLine(line);
                count++;
            }

            writer.Flush();
            return Result<int>.Ok(count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<int>.Fail(Errors.ExportFailed(target, ex.Message));
        }
    }

    public static Result<int> WriteFile(string path, IEnumerable<string> lines)
    {
        string temp = path + ".tmp";
        try
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (dir is not null) Directory.CreateDirectory(dir);

            var written = Write(
                () => new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None),
                lines, path);

            if (written is Result<int>.Success)
            {
                File.Move(temp, path, overwrite: true);
                return written;
            }

            File.Delete(temp);
            return written;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<int>.Fail(Errors.ExportFailed(path, ex.Message));
        }
    }
}