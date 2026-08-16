using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Features.Birthday;
using Neuvillette.Characters.Neuvillette.Relics;
using Neuvillette.Characters.Neuvillette.Timeline;

namespace Neuvillette.Characters.Neuvillette;

public class NeuvilletteRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "neuvillette";
    public override Color LabOutlineColor => Neuvillette.Color;
    public override string BigEnergyIconPath => BirthdayEnergyIcons.BigIconPath("big_energy.png");
    public override string TextEnergyIconPath => BirthdayEnergyIcons.TextIconPath("text_energy.png");

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        var relics = AllRelics.ToList();
        if (!unlockState.IsEpochRevealed<Neuvillette3Epoch>())
            relics.RemoveAll(relic => relic is Gavel or ExcuseNote or SeaFoamMailbox);
        if (!unlockState.IsEpochRevealed<Neuvillette5Epoch>())
            relics.RemoveAll(relic => relic is StoppedPocketWatch or Monocle or Plumule);
        return relics;
    }
}
