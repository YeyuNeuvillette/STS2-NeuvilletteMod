using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class SelfInflicted() : NeuvilletteCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new ExtraDamageVar(1m),
         new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel _, Creature? target) =>
         {
             if (target?.Monster == null)
                 return 0m;

             var attackIntent = target.Monster.NextMove.Intents.OfType<AttackIntent>().FirstOrDefault();
             return attackIntent?.GetSingleDamage(new[] { target }, target) ?? 0m;
         }),
         new CalculatedVar("CalculatedHits").WithMultiplier(static (CardModel _, Creature? target) =>
        {
            if (target?.Monster == null)
                return 1m;

            var attackIntent = target.Monster.NextMove.Intents.OfType<AttackIntent>().FirstOrDefault();
            return attackIntent == null ? 1m : Math.Max(attackIntent.Repeats, 1);
        })
    ];

    protected override bool ShouldGlowGoldInternal => CombatState?.HittableEnemies.Any(static creature =>
        creature.Monster?.NextMove.Intents.Any(static intent =>
            intent.IntentType == IntentType.Attack || intent.IntentType == IntentType.DeathBlow) == true) == true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var target = cardPlay.Target;
        var attackIntent = target.Monster?.NextMove.Intents.OfType<AttackIntent>().FirstOrDefault();
        var hitDamage = attackIntent?.GetSingleDamage(new[] { target }, target) ?? 0m;
        var repeats = attackIntent == null ? 1 : Math.Max(attackIntent.Repeats, 1);
        if (hitDamage <= 0)
            return;

        await DamageCmd.Attack(hitDamage)
            .WithHitCount(repeats)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}