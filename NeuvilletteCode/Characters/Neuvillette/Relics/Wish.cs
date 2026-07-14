using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class Wish : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int MerchantCost => 100;

    public override bool IsAllowedInShops => false;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        var owner = Owner;
        if (owner != null && participants.Contains(owner.Creature) && owner.PlayerCombatState is { TurnNumber: <= 1 })
        {
            Flash();
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, owner);
        }
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        foreach (var opt in options)
        {
            if (opt.OptionId == "NEUVILLETTE_FORGE_SWORD")
                return false;
        }
        options.Add(new ForgeSwordRestSiteOption(player));
        return true;
    }
}