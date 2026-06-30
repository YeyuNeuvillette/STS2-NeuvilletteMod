using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class BottledSandCavern : BaseRelic
{
    private int _progressCounter;
    private bool _killTriggered;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool ShowCounter => true;
    public override int DisplayAmount => _progressCounter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<SandVortex>());

    public override Task BeforeCombatStart()
    {
        _progressCounter = 0;
        _killTriggered = false;
        return Task.CompletedTask;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || combatState.RoundNumber != 1)
            return;

        Flash();

        var cards = new List<CardModel>();
        for (int i = 0; i < 6; i++)
        {
            cards.Add(combatState.CreateCard<SandVortex>(Owner));
        }

        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(results, 2f);
        await Cmd.Wait(0.75f);
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);

        if (player != Owner)
            return;

        _progressCounter++;
        InvokeDisplayAmountChanged();

        if (_progressCounter < 30 || _killTriggered)
            return;

        var creature = Owner?.Creature;
        if (creature == null)
            return;

        _killTriggered = true;
        Flash();

        var enemies = creature.CombatState?.Enemies.Where(e => !e.IsDead).ToList();
        if (enemies is { Count: > 0 })
            await CreatureCmd.Kill(enemies);
    }

    public async Task IncrementProgressCounter()
    {
        _progressCounter++;
        InvokeDisplayAmountChanged();

        if (_progressCounter < 30 || _killTriggered)
            return;

        var creature = Owner?.Creature;
        if (creature == null)
            return;

        _killTriggered = true;
        Flash();

        var enemies = creature.CombatState?.Enemies.Where(e => !e.IsDead).ToList();
        if (enemies is { Count: > 0 })
            await CreatureCmd.Kill(enemies);
    }
}