using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Neuvillette.Scripts;
namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class WaterSplash() : NeuvilletteCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(3m),
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("SurgeNextTurn", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
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
        }

        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
        await PowerCmd.Apply<SurgeNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["SurgeNextTurn"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["SurgeNextTurn"].UpgradeValueBy(3m);
    }
}
