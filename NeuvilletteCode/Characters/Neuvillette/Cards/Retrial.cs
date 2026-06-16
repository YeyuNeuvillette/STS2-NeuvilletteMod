using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Logging;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Retrial() : NeuvilletteCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.SourcewaterDroplet];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<VulnerablePower>()]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(11m, ValueProp.Move),
        new DynamicVar("VulnerableAmount", 1m)
    ];

    protected override bool ShouldGlowGoldInternal => (Owner?.Creature.GetPowerAmount<SourcewaterDroplet>() ?? 0m) >= 2m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var droplets = Owner.Creature.GetPower<SourcewaterDroplet>();
        var hasDroplets = droplets != null && droplets.Amount >= 2m;
        var target = hasDroplets ? cardPlay.Target : Owner.Creature;

        if (hasDroplets)
            await PowerCmd.ModifyAmount(choiceContext, droplets!, -2m, Owner.Creature, this);

        var vulnerableAmount = DynamicVars["VulnerableAmount"].BaseValue;
        if (IsUpgraded && target == Owner.Creature)
            vulnerableAmount -= 1m;

        Log.Info($"[Retrial] Applying VulnerablePower to {target.Name} (Side: {target.Side}), amount: {vulnerableAmount}");
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, vulnerableAmount, Owner.Creature, this);
        
        var appliedVulnerable = target.GetPower<VulnerablePower>();
        if (appliedVulnerable != null)
        {
            Log.Info($"[Retrial] VulnerablePower applied - Amount: {appliedVulnerable.Amount}, SkipNextDurationTick: {appliedVulnerable.SkipNextDurationTick}");
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["VulnerableAmount"].UpgradeValueBy(1m);
    }
}