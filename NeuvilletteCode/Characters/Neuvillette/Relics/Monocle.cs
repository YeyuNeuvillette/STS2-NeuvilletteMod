using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Extensions;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Monocle : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);

        if (Owner?.Creature == null)
            return;

        var creature = Owner.Creature;
        var hpThreshold = creature.MaxHp * 0.2m;

        if (creature.CurrentHp > hpThreshold)
            return;

        var upgradableCards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c != null && c.IsUpgradable)
            .ToList();

        if (upgradableCards.Count == 0)
            return;

        var randomCard = upgradableCards.StableShuffle(Owner.RunState.Rng.Niche).First();
        CardCmd.Upgrade(randomCard);
    }
}