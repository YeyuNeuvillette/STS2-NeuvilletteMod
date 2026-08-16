using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.HoverTips;

using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Neuvillette.Scripts;
namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Punishment() : NeuvilletteCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromPower<SurgePower>()
        ]);
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var surgeAmount = Owner.Creature.GetPower<SurgePower>()?.Amount ?? 0m;
        if (surgeAmount <= 0)
            return;

        await DamageCmd.Attack(surgeAmount)
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

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
