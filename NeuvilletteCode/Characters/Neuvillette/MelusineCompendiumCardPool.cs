using Godot;
using Neuvillette.Characters.Neuvillette.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Characters.Neuvillette;

/// <summary>
/// Compendium-only view of the standard Melusine Stickers. Sigewinne Sticker
/// remains a special generated card and is intentionally hidden from the library.
/// </summary>
[RegisterSharedCardPool]
public sealed class MelusineCompendiumCardPool : TypeListCardPoolModel
{
    public override string Title => "MelusineCompendium";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;

#pragma warning disable CS0618
    [Obsolete]
    protected override IEnumerable<Type> CardTypes => typeof(MelusineStickerCard).Assembly
        .GetTypes()
        .Where(static type =>
            !type.IsAbstract &&
            type.IsSubclassOf(typeof(MelusineStickerCard)) &&
            type != typeof(SigewinneSticker));
#pragma warning restore CS0618
}
