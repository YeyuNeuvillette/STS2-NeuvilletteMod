using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class DragonTreasurePower : NeuvillettePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player == null)
            return;

        Flash();

        room.AddExtraReward(Owner.Player, new GoldReward(20, Owner.Player));
        room.AddExtraReward(Owner.Player, new RelicReward(Owner.Player));
        room.AddExtraReward(Owner.Player, new PotionReward(Owner.Player));
        await CreatureCmd.GainMaxHp(Owner, 3m);

        var dragonTreasureCard = Owner.Player.Deck.Cards.FirstOrDefault(static c => c.Id == ModelDb.GetId<DragonTreasure>());
        if (dragonTreasureCard != null)
            await CardPileCmd.RemoveFromDeck(dragonTreasureCard);
    }
}