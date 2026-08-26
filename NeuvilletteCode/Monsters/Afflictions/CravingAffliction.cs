using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Neuvillette.Monsters.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Neuvillette.Monsters.Afflictions;

[RegisterAffliction]
public sealed class CravingAffliction : AfflictionModel, IModAfflictionAssetOverrides
{
    private const string CravingOverlayScenePath = "res://Neuvillette/scenes/cards/overlays/afflictions/craving.tscn";

    public AfflictionAssetProfile AssetProfile => new(CravingOverlayScenePath);

    public string CustomOverlayScenePath => CravingOverlayScenePath;

    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromCard<RiftCard>() };

    public override void AfterApplied()
    {
        Card?.AddKeyword(CardKeyword.Ethereal);
        if (Card != null)
        {
            NCard.FindOnTable(Card)?.UpdateVisuals(Card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
        }
    }

    public override void BeforeRemoved()
    {
        Card?.RemoveKeyword(CardKeyword.Ethereal);
        if (Card != null)
        {
            NCard.FindOnTable(Card)?.UpdateVisuals(Card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
        }
    }
}
