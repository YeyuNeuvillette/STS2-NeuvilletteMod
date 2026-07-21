using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Neuvillette.Characters.Neuvillette.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class PlumulePower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<Plumule>();
}
