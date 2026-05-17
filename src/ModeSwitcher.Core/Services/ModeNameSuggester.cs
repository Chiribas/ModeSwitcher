using System.Text.RegularExpressions;

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
}
