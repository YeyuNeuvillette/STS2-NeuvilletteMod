using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Neuvillette.Features.Birthday;

namespace Neuvillette.Scripts;

internal partial class NeuvilletteEnergyCounter : NEnergyCounter
{
    public override void _Ready()
    {
        base._Ready();

        if (!BirthdayEnergyIcons.IsActive)
            return;

        var layer = GetNodeOrNull<TextureRect>("Layers/Layer1");
        var birthdayTexture = ResourceLoader.Load<Texture2D>(BirthdayEnergyIcons.CounterTexturePath);
        if (layer != null && birthdayTexture != null)
            layer.Texture = birthdayTexture;
    }
}
