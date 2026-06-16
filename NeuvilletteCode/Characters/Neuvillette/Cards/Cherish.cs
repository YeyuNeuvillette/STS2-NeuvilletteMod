using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Cherish() : NeuvilletteCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];
    protected override bool ShouldGlowGoldInternal => HasMelusineStickerInHand();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move)
    ];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card != this)
            return base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);

        var isModified = base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);
        if (!isModified)
            modifiedCost = originalCost;

        var stickerCount = GetMelusineStickerCountInHand();
        if (stickerCount <= 0)
            return isModified;

        modifiedCost = Math.Max(0m, modifiedCost - stickerCount);
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

    private bool HasMelusineStickerInHand()
    {
        var hand = Owner?.PlayerCombatState?.Hand;
        return hand != null && hand.Cards.Any(static card => card.Pool is MelusineCardPool);
    }

    private int GetMelusineStickerCountInHand()
    {
        var hand = Owner?.PlayerCombatState?.Hand;
        return hand?.Cards.Count(static card => card.Pool is MelusineCardPool) ?? 0;
    }
}