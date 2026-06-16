using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Potions;

[RegisterPotion(typeof(NeuvillettePotionPool))]
public sealed class BottledTicket : BasePotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ContemptOfCourtPower>(8m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<ContemptOfCourtPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("4fc3f7"));
        var contemptVar = base.DynamicVars.Values.FirstOrDefault(v => v is PowerVar<ContemptOfCourtPower>);
        if (contemptVar != null)
        {
            await PowerCmd.Apply<ContemptOfCourtPower>(choiceContext, target, contemptVar.BaseValue, base.Owner.Creature, null);
        }
    }
}