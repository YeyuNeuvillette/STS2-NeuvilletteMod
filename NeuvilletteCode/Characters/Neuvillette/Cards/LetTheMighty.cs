using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class LetTheMighty() : NeuvilletteCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MightyDamageVar(9m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/Neuvillette/sfx/LetTheMighty");

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(RunState);
        ArgumentNullException.ThrowIfNull(CombatState);

        var multiplier = GetTargetMultiplier(this, cardPlay.Target);

        var modifiedDamage = Hook.ModifyDamage(
            RunState,
            CombatState,
            cardPlay.Target,
            Owner.Creature,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            this,
            cardPlay,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out IEnumerable<AbstractModel> _);

        var finalDamage = modifiedDamage * multiplier;

        await DamageCmd.Attack(finalDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Unpowered()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private static decimal GetTargetMultiplier(CardModel card, Creature? target)
    {
        if (target?.IsPrimaryEnemy != true)
            return 1m;

        return card.Owner.RunState.CurrentRoom?.RoomType switch
        {
            RoomType.Boss => 4m,
            RoomType.Elite => 2m,
            _ => 1m,
        };
    }

    private sealed class MightyDamageVar(decimal damage, ValueProp props) : DamageVar(damage, props)
    {
        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            PreviewValue *= GetTargetMultiplier(card, target);
        }
    }
}
