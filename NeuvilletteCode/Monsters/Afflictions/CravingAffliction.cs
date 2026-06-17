using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Monsters.Afflictions;

[RegisterAffliction]
public sealed class CravingAffliction : AfflictionModel
{
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