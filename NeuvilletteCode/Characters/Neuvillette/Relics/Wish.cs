using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Wish : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int MerchantCost => 100;

    public override bool IsAllowedInShops => false;

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