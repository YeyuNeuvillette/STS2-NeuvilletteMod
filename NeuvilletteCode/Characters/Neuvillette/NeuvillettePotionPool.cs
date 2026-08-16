using Godot;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Features.Birthday;

namespace Neuvillette.Characters.Neuvillette;

public class NeuvillettePotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "neuvillette";
    public override Color LabOutlineColor => Neuvillette.Color;
    public override string BigEnergyIconPath => BirthdayEnergyIcons.BigIconPath("big_energy.png");
    public override string TextEnergyIconPath => BirthdayEnergyIcons.TextIconPath("text_energy.png");
}
