using MegaCrit.Sts2.Core.Entities.Powers;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class InsatiableHungerPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}