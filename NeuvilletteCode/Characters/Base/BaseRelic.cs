using Godot;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Extensions;

namespace Neuvillette.Characters.Base;

public abstract class BaseRelic : ModRelicTemplate
{
    private string ArtFileName => $"{Id.Entry.ToRelicArtFileName()}.png";
    private string LegacyArtFileName => $"{Id.Entry.ToLegacyCompactFileName()}.png";
    private string OutlineFileName => $"{Id.Entry.ToRelicArtFileName()}_outline.png";
    private string LegacyOutlineFileName => $"{Id.Entry.ToLegacyCompactFileName()}_outline.png";

    public override string? CustomIconPath =>
        ResolveExistingPath(
            ArtFileName.RelicImagePath(),
            LegacyArtFileName.RelicImagePath(),
            "relic.png".RelicImagePath());

    public override string? CustomIconOutlinePath =>
        ResolveExistingPath(
            OutlineFileName.RelicImagePath(),
            LegacyOutlineFileName.RelicImagePath(),
            "relic_outline.png".RelicImagePath());

    public override string? CustomBigIconPath =>
        ResolveExistingPath(
            ArtFileName.BigRelicImagePath(),
            ArtFileName.RelicImagePath(),
            LegacyArtFileName.BigRelicImagePath(),
            LegacyArtFileName.RelicImagePath(),
            "relic.png".BigRelicImagePath(),
            "relic.png".RelicImagePath());

    private static string ResolveExistingPath(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (ResourceLoader.Exists(candidate))
                return candidate;
        }

        return candidates[^1];
    }
}
