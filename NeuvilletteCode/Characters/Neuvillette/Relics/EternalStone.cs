using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Enchantments;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class EternalStone : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromEnchantment<Heavy>());

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        if (Owner?.Deck == null)
            return;

        var heavyEnchantment = ModelDb.Enchantment<Heavy>();
        var attackCards = Owner.Deck.Cards
            .Where(card => card.Type == CardType.Attack && heavyEnchantment.CanEnchant(card))
            .ToList();

        if (attackCards.Count == 0)
            return;

        var randomCard = Owner.RunState.Rng.Niche.NextItem(attackCards);
        if (randomCard == null)
            return;
        
        CardCmd.Enchant<Heavy>(randomCard, 1);
        
        var enchantVfx = NCardEnchantVfx.Create(randomCard);
        if (enchantVfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(enchantVfx);
        }
    }
}