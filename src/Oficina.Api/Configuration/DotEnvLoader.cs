namespace Oficina.Api.Configuration;

internal static class DotEnvLoader
{
    public static void LoadFromProjectRoot()
    {
        var file = FindFile(Directory.GetCurrentDirectory());
        if (file is null)
        {
            return;
        }

        foreach (var line in File.ReadLines(file))
        {
            var entry = line.Trim();
            if (string.IsNullOrWhiteSpace(entry) || entry.StartsWith('#'))
            {
                continue;
            }

            if (entry.StartsWith("export ", StringComparison.Ordinal))
            {
                entry = entry[7..].TrimStart();
            }

            var separator = entry.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = entry[..separator].Trim();
            var value = Unquote(entry[(separator + 1)..].Trim());
            if (!string.IsNullOrWhiteSpace(key) && Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindFile(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string Unquote(string value)
    {
        return value.Length >= 2 &&
               ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            ? value[1..^1]
            : value;
    }
}
