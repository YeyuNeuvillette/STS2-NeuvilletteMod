using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class Memory : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DexterityPower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner!.Creature, 1m, Owner.Creature, null);
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