using System.Text.RegularExpressions;

public static class RegexPatterns
{
    public static readonly Regex FNumberRegex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);
    public static readonly Regex NBT_TagRegex = new Regex(@"Tags:\[([^\]]+)\]");
    public static readonly Regex NBT_UUIDRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");
    public static readonly Regex UuidExtractedFormatRegex =
        new Regex(@"^(-?\d+),(-?\d+),(-?\d+),(-?\d+)$", RegexOptions.Compiled);

    public static readonly Regex TagZeroEndRegex = new Regex(@".*\D0$", RegexOptions.Compiled);

    public static readonly Regex VersionRegex = new Regex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);
}
