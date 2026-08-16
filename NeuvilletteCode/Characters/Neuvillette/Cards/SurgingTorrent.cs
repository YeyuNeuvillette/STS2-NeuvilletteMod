using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Neuvillette.Scripts;
namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class SurgingTorrent() : NeuvilletteCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.SourcewaterDroplet];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(7m),
        new ExtraDamageVar(5m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
            card.Owner?.Creature.GetPowerAmount<SourcewaterDroplet>() ?? 0m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", "event:/Neuvillette/sfx/WaterSplashHit")
            .BeforeDamage(() =>
            {
                var vfx = NeuvilletteAttackVfx.Create(cardPlay.Target);
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
                return Task.CompletedTask;
            })
            .Execute(choiceContext);

        var droplets = Owner.Creature.GetPower<SourcewaterDroplet>();
        if (droplets != null && droplets.Amount > 0)
            await PowerCmd.Remove(droplets);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2m);
        DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}
