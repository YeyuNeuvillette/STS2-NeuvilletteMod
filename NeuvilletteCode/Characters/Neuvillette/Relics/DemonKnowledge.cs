using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class DemonKnowledge : BaseRelic, IModRightClickableRelic
{
    private const int MaxTotalUses = 13;

    [SavedProperty]
    public int DemonKnowledge_TotalUses { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => MaxTotalUses - DemonKnowledge_TotalUses;

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        if (!CombatManager.Instance.IsInProgress)
            return false;
        if (context.Player != base.Owner)
            return false;
        return true;
    }

    private static bool CanAfford(CardModel card)
    {
        return card.CanPlay(out _, out _);
    }

    private List<CardModel> ShuffleTake(List<CardModel> list, int count)
    {
        var rng = base.Owner.RunState.Rng.CombatCardGeneration;
        rng.Shuffle(list);
        return list.Take(count).ToList();
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (DemonKnowledge_TotalUses >= MaxTotalUses)
        {
            TalkCmd.Play(new LocString("relics", "NEUVILLETTE_RELIC_DEMON_KNOWLEDGE.dialogue_exhausted"), base.Owner.Creature, VfxColor.Red);
            return;
        }

        var cardPool = base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint).ToList();
        List<CardModel> allCards = CardFactory.GetDistinctForCombat(base.Owner, cardPool, cardPool.Count, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
        List<CardModel> affordable = allCards.Where(CanAfford).ToList();

        if (affordable.Count == 0)
        {
            Log.Warn("[DemonKnowledge] No affordable cards available for selection.");
            return;
        }

        List<CardModel> choices = affordable.Count <= 2
            ? affordable
            : ShuffleTake(affordable, 2);

        Flash();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(context.PlayerChoiceContext!, choices, base.Owner, canSkip: true);

        DemonKnowledge_TotalUses++;
        InvokeDisplayAmountChanged();

        if (selected != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner);
            TalkCmd.Play(new LocString("relics", "NEUVILLETTE_RELIC_DEMON_KNOWLEDGE.dialogue"), base.Owner.Creature, VfxColor.Purple);
        }
    }
}