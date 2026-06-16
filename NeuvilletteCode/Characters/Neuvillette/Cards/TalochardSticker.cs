using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(MelusineCardPool))]
public sealed class TalochardSticker() : MelusineStickerCard(TargetType.AnyEnemy)
{
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromPower<ArtifactPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        ]);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await CreatureCmd.LoseBlock(cardPlay.Target, cardPlay.Target.Block);
        if (cardPlay.Target.HasPower<ArtifactPower>())
            await PowerCmd.Remove<ArtifactPower>(cardPlay.Target);

        await CardPileCmd.Draw(choiceContext, (int)DynamicVars["Cards"].BaseValue, Owner);
    }
}
