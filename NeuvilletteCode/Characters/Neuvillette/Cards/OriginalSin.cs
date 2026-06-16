using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class OriginalSin() : NeuvilletteCard(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ContemptOfCourtPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<ContemptOfCourtPower>()]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || CombatState.HittableEnemies.Count == 0)
            return;

        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<ContemptOfCourtPower>(choiceContext, enemy, DynamicVars["ContemptOfCourtPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ContemptOfCourtPower"].UpgradeValueBy(1m);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);

        if (player != Owner || combatState.RoundNumber != 1 || player.PlayerCombatState == null)
            return;

        player.PlayerCombatState.DrawPile.MoveToBottomInternal(this);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner != player || CombatState?.RoundNumber != 1 || Owner.PlayerCombatState == null)
            return;

        if (!Owner.PlayerCombatState.DrawPile.Cards.Contains(this))
            return;

        await CardCmd.AutoPlay(choiceContext, this, null);
    }
}