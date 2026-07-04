using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class WarmCurrent() : NeuvilletteCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];
    protected override bool ShouldGlowGoldInternal => HasMelusineStickerInHand();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var hand = Owner?.PlayerCombatState?.Hand;
        if (hand == null)
            return;

        var stickerCards = hand.Cards
            .Where(card => card.Pool is MelusineCardPool)
            .ToList();

        foreach (var sticker in stickerCards)
        {
            var copy = Owner?.RunState?.CloneCard(sticker);
            if (copy != null)
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private bool HasMelusineStickerInHand()
    {
        var hand = Owner?.PlayerCombatState?.Hand;
        return hand != null && hand.Cards.Any(static card => card.Pool is MelusineCardPool);
    }
}