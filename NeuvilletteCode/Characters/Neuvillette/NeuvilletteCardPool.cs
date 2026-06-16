using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;
using Neuvillette.Extensions;

namespace Neuvillette.Characters.Neuvillette;

public class NeuvilletteCardPool : TypeListCardPoolModel
{
    public override string Title => Neuvillette.CharacterId;
    public override string EnergyColorName => "neuvillette";
    public override string BigEnergyIconPath => "charui/energy_neuvillette_big.png".ImagePath();
    public override string TextEnergyIconPath => "charui/energy_neuvillette.png".ImagePath();
    public override Material? PoolFrameMaterial => MaterialUtils.CreateHsvShaderMaterial(0.58f, 0.73f, 0.93f);
    public override Color DeckEntryCardColor => Neuvillette.Color;
    public override bool IsColorless => false;
}
