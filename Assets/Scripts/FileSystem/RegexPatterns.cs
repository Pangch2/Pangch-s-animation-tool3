using System.Text.RegularExpressions;

public static class RegexPatterns
{
    public static readonly Regex FNumberRegex = new Regex(@"^f\d+\.mcfunction$", RegexOptions.IgnoreCase);
    public static readonly Regex NBT_TagRegex = new Regex(@"Tags:\[([^\]]+)\]");
    public static readonly Regex NBT_UUIDRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");
}
