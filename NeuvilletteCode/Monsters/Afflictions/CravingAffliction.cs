using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Monsters.Afflictions;

[RegisterAffliction]
public sealed class CravingAffliction : AfflictionModel
{
    public override bool HasExtraCardText => true;
}