using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(MelusineCardPool))]
public sealed class MentheSticker() : MelusineStickerCard(TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var eligibleCards = ModelDb.CardPool<NeuvilletteCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.EnergyCost?.Canonical > 1 || card.EnergyCost?.CostsX == true);

        foreach (var card in CardFactory.GetForCombat(Owner, eligibleCards, 1, Owner.RunState.Rng.CombatCardGeneration).ToList())
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}