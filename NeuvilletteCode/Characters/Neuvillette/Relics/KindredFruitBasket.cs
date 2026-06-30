using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class KindredFruitBasket : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips
            .Concat(HoverTipFactory.FromRelic<Strawberry>())
            .Concat(HoverTipFactory.FromRelic<Pear>())
            .Concat(HoverTipFactory.FromRelic<Mango>());

    public override async Task AfterObtained()
    {
        await RelicCmd.Obtain<Strawberry>(Owner);
        await RelicCmd.Obtain<Pear>(Owner);
        await RelicCmd.Obtain<Mango>(Owner);
    }
}