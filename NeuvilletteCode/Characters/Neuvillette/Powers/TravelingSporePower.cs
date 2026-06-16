using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Neuvillette.Characters.Neuvillette.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class TravelingSporePower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetCardsToRemove();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new TravelingSporePowerCardsVar(this)];

    private int GetCardsToRemove()
    {
        if (base.Owner?.Player?.RunState != null)
        {
            return 1 + base.Owner.Player.RunState.CurrentActIndex;
        }
        return 1;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var player = base.Owner?.Player;
        if (player != null && player.RunState != null)
        {
            int currentActIndex = player.RunState.CurrentActIndex;
            int cardsToRemove = 1 + currentActIndex;
            for (int i = 0; i < cardsToRemove; i++)
            {
                room.AddExtraReward(player, new CardRemovalReward(player));
            }

            var travelingSporeCard = player.Deck.Cards.FirstOrDefault(static c => c.Id == ModelDb.GetId<TravelingSpore>());
            if (travelingSporeCard != null)
            {
                await CardPileCmd.RemoveFromDeck(travelingSporeCard);
            }
        }
    }
}

public sealed class TravelingSporePowerCardsVar : DynamicVar
{
    private readonly TravelingSporePower _power;

    public const string defaultName = "Cards";

    public TravelingSporePowerCardsVar(TravelingSporePower power) : base("Cards", 1)
    {
        _power = power;
    }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        UpdateValue();
    }

    private void UpdateValue()
    {
        if (_power.Owner?.Player?.RunState != null)
        {
            int currentActIndex = _power.Owner.Player.RunState.CurrentActIndex;
            BaseValue = 1 + currentActIndex;
        }
        else
        {
            BaseValue = 1;
        }
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        UpdateValue();
    }
}