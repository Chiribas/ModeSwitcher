using System.Text.Json;
using System.Text.RegularExpressions;
using ModeSwitcher.Core.FileSystem;

namespace ModeSwitcher.Core.Services;

public static class ModeNameSuggester
{
    private static readonly Regex InvalidChars = new(@"[^A-Za-z0-9._\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleUnderscores = new("_+", RegexOptions.Compiled);

    public static string ToFolderName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        var replaced = InvalidChars.Replace(displayName, "_");
        var collapsed = MultipleUnderscores.Replace(replaced, "_");
        return collapsed.Trim('_');
    }

    public static string? SuggestFromSettings(string settingsJsonPath, IFileSystem fileSystem)
    {
        try
        {
            using var stream = fileSystem.OpenRead(settingsJsonPath);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            var url = TryGetStringDeep(root, "env", "ANTHROPIC_BASE_URL");
            if (url is null) return null;

            var model = TryGetStringDeep(root, "env", "model")
                        ?? TryGetStringDeep(root, "model");
            if (model is null) return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
            return $"{uri.Host} ({model})";
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetStringDeep(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(key, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }
}
