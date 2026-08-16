using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Neuvillette.Scripts;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Downpour() : NeuvilletteCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(4)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithAttackerAnim("Cast", 3.5f)
            .WithHitFx(null, "event:/Neuvillette/sfx/WaterSplashHit")
            .BeforeDamage(() =>
            {
                var rainVfx = DownpourRainVfx.Create();
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(rainVfx);

                foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature))
                {
                    if (!enemy.IsAlive)
                        continue;

                    var splashVfx = NeuvilletteAttackVfx.Create(enemy);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(splashVfx);
                }

                return Task.CompletedTask;
            })
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}
