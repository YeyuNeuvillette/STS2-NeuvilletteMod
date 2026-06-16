using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class ShatteredCrown : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player != Owner ? amount : amount + 1m;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        int originalCount = rewards.Count;
        List<Reward> filteredRewards = rewards.Where(r => !(r is CardReward)).ToList();
        
        rewards.Clear();
        rewards.AddRange(filteredRewards);
        
        return rewards.Count != originalCount;
    }
}