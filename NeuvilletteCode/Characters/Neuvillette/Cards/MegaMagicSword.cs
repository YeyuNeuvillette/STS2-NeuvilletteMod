using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class MegaMagicSword() : NeuvilletteCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(16m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
        {
            var owner = card.Owner;
            var combatState = owner?.Creature?.CombatState;
            if (owner == null || combatState == null)
                return 0m;

            return PileType.Exhaust.GetPile(owner).Cards.Count(cardInExhaust => cardInExhaust is MelusineStickerCard);
        })
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            var stickers = PileType.Exhaust.GetPile(Owner).Cards
                .Where(card => card is MelusineStickerCard)
                .ToList();

            using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
            {
                foreach (var sticker in stickers)
                {
                    if (IsUpgraded)
                        CardCmd.Upgrade(sticker);

                    var autoPlayTarget = sticker.TargetType == TargetType.Self ? Owner.Creature : cardPlay.Target;
                    await CardCmd.AutoPlay(choiceContext, sticker, autoPlayTarget, AutoPlayType.Default, skipCardPileVisuals: true);
                }
            }
        }

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "heavy_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}