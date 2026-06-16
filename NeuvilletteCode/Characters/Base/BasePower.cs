using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Base;

public abstract class BasePower : ModPowerTemplate
{
    private string ArtFileName => $"{Id.Entry.ToPowerArtFileName()}.png";
    private string LegacyArtFileName => $"{Id.Entry.ToLegacyCompactFileName()}.png";

    public override string? CustomIconPath =>
        ResolveExistingPath(
            ArtFileName.PowerImagePath(),
            LegacyArtFileName.PowerImagePath(),
            "power.png".PowerImagePath());

    public override string? CustomBigIconPath =>
        ResolveExistingPath(
            ArtFileName.BigPowerImagePath(),
            ArtFileName.PowerImagePath(),
            LegacyArtFileName.BigPowerImagePath(),
            LegacyArtFileName.PowerImagePath(),
            "power.png".BigPowerImagePath(),
            "power.png".PowerImagePath());

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Amount < 0 && !power.AllowNegative)
            power.RemoveInternal();

        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
    }

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