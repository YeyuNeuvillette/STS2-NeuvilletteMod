using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Neuvillette.Monsters.Afflictions;

[RegisterAffliction]
public sealed class CravingAffliction : AfflictionModel
{
    private static readonly PackedScene? PlaceholderOverlay = ResourceLoader.Load<PackedScene>(
        "res://scenes/cards/overlays/afflictions/hexed.tscn", null, ResourceLoader.CacheMode.Reuse);

    static CravingAffliction()
    {
        ExternalAssetOverrideRegistry.RegisterAfflictionOverlaySceneProvider(
            nameof(CravingAffliction),
            affliction => affliction is CravingAffliction ? PlaceholderOverlay : null);
    }

    public override bool HasExtraCardText => true;

    public override void AfterApplied()
    {
        Card?.AddKeyword(CardKeyword.Ethereal);
    }

    public override void BeforeRemoved()
    {
        Card?.RemoveKeyword(CardKeyword.Ethereal);
    }
}