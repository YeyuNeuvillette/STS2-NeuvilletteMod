using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Relics;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(EventCardPool))]
public sealed class SandVortex() : NeuvilletteCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var relic = Owner?.GetRelic<BottledSandCavern>();
        if (relic != null)
        {
            await relic.IncrementProgressCounter();
        }

        EnergyCost.AddThisCombat(1);
    }
}