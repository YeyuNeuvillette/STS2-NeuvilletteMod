using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Neuvillette.Characters.Neuvillette.Relics;
using Neuvillette.Extensions;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class PlumulePower : TemporaryStrengthPower, IModPowerAssetOverrides
{
    private const string IconFileName = "neuvillette_power_plumule_power.png";

    public override AbstractModel OriginModel => ModelDb.Relic<Plumule>();

    public PowerAssetProfile AssetProfile { get; } = PowerAssetProfile.Empty;

    public string? CustomIconPath => IconFileName.PowerImagePath();

    public string? CustomBigIconPath => IconFileName.BigPowerImagePath();
}
