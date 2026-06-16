using Godot;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Extensions;

namespace Neuvillette.Characters.Neuvillette;

public class NeuvilletteRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "neuvillette";
    public override Color LabOutlineColor => Neuvillette.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
