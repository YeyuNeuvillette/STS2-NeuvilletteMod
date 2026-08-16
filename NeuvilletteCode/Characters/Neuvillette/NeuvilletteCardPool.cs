using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;
using Neuvillette.Features.Birthday;

namespace Neuvillette.Characters.Neuvillette;

public class NeuvilletteCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public override string Title => Neuvillette.CharacterId;
    public override string EnergyColorName => "neuvillette";
    public override string BigEnergyIconPath => BirthdayEnergyIcons.BigIconPath("energy_neuvillette_big.png");
    public override string TextEnergyIconPath => BirthdayEnergyIcons.TextIconPath("energy_neuvillette.png");
    public override Material? PoolFrameMaterial => MaterialUtils.CreateHsvShaderMaterial(0.58f, 0.73f, 0.93f);
    public override Color DeckEntryCardColor => Neuvillette.Color;
    public override bool IsColorless => false;
}
