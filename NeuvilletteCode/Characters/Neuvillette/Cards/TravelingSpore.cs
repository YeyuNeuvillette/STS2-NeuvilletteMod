using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class TravelingSpore() : NeuvilletteCard(2, CardType.Power, CardRarity.Event, TargetType.Self)
{

    protected override IEnumerable<DynamicVar> CanonicalVars => [new TravelingSporeCardsVar()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<TravelingSporePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

public sealed class TravelingSporeCardsVar : DynamicVar
{
    public const string defaultName = "Cards";

    public TravelingSporeCardsVar() : base("Cards", 1)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        if (card.Owner?.RunState != null)
        {
            int currentActIndex = card.Owner.RunState.CurrentActIndex;
            BaseValue = 1 + currentActIndex;
        }
        else
        {
            BaseValue = 1;
        }
    }
}