using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class FinalJudgment() : NeuvilletteCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, false)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var currentRoom = Owner.RunState.CurrentRoom;
        var isBoss = currentRoom?.RoomType == RoomType.Boss && cardPlay.Target.IsPrimaryEnemy;
        
        
        if (isBoss)
        {
            var hpLoss = Math.Floor(cardPlay.Target.CurrentHp / 2m);
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered, this);
            return;
        }

        await CreatureCmd.Kill(cardPlay.Target, true);
    }
}