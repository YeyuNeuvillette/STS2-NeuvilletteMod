using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        new DamageVar(11m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/Neuvillette/sfx/LetTheMighty");

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(RunState);
        ArgumentNullException.ThrowIfNull(CombatState);

        var currentRoom = Owner.RunState.CurrentRoom;
        var isBoss = currentRoom?.RoomType == RoomType.Boss && cardPlay.Target.IsPrimaryEnemy;
        var isElite = currentRoom?.RoomType == RoomType.Elite;

        var multiplier = isBoss ? 4m : (isElite ? 2m : 1m);

        var modifiedDamage = Hook.ModifyDamage(
            RunState,
            CombatState,
            cardPlay.Target,
            Owner.Creature,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            this,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out IEnumerable<AbstractModel> _);

        var finalDamage = modifiedDamage * multiplier;

        await DamageCmd.Attack(finalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}