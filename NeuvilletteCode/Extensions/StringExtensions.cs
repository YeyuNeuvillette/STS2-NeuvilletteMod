namespace Neuvillette.Extensions;

public static class StringExtensions
{
    public static string ImagePath(this string path) => BuildResPath("images", path);
    public static string CharacterImgPath(this string path, string character) => BuildResPath("images", "characters", character, path);
    public static string CharacterScenePath(this string path, string character) => BuildResPath("scenes", character, path);
    public static string CardsImagePath(this string path) => BuildResPath("images", "cards", path);
    public static string BigCardsImagePath(this string path) => BuildResPath("images", "cards", "big", path);
    public static string CardBetaImagePath(this string path) => BuildResPath("images", "cards", "beta", path);
    public static string PowerImagePath(this string path) => BuildResPath("images", "powers", path);
    public static string BigPowerImagePath(this string path) => BuildResPath("images", "powers", "big", path);
    public static string RelicImagePath(this string path) => BuildResPath("images", "relics", path);
    public static string BigRelicImagePath(this string path) => BuildResPath("images", "relics", "big", path);
    public static string PotionImagePath(this string path) => BuildResPath("images", "potions", path);
    public static string BigPotionImagePath(this string path) => BuildResPath("images", "potions", "big", path);
    public static string OrbImgPath(this string path) => BuildResPath("images", "orbs", path);
    public static string OrbScenePath(this string path) => BuildResPath("scenes", "orbs", path);
    public static string CharacterUiPath(this string path) => BuildResPath("images", "charui", path);

    public static string ToCardArtFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.ToLowerInvariant();
    }

    public static string ToPowerArtFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.ToLowerInvariant();
    }

    public static string ToRelicArtFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.ToLowerInvariant();
    }

    public static string ToOrbArtFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.ToLowerInvariant();
    }

    public static string ToPotionArtFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.ToLowerInvariant();
    }

    public static string ToLegacyCompactFileName(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return string.Concat(value.RemovePrefix().Where(static c => c != '_')).ToLowerInvariant();
    }

    public static string ToSnakeCase(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && value[i - 1] != '_')
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    public static string RemovePrefix(this string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var index = value.LastIndexOf('_');
        return index >= 0 && index < value.Length - 1 ? value[(index + 1)..] : value;
    }

    private static string BuildResPath(params string[] segments)
    {
        return string.Join("/", new[] { MainFile.ResPath.TrimEnd('/') }
            .Concat(segments.Select(NormalizeSegment).Where(static s => !string.IsNullOrEmpty(s))));
    }

    private static string NormalizeSegment(string segment) => segment.Replace('\\', '/').Trim('/');
}