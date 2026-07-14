using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class NarzissenkreuzSword : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
}