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
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Potions;

[RegisterPotion(typeof(NeuvillettePotionPool))]
public sealed class WaterBottle : BasePotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SurgePower>(10m)
    ];

    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.Surge
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("4fc3f7"));
        var surgeVar = base.DynamicVars.Values.FirstOrDefault(v => v is PowerVar<SurgePower>);
        if (surgeVar != null)
        {
            var livingWaterAmount = target.GetPowerAmount<LivingWaterPower>();
            var totalSurge = surgeVar.BaseValue + livingWaterAmount;

            await CreatureCmd.Heal(target, totalSurge);
            await PowerCmd.Apply<SurgePower>(choiceContext, target, totalSurge, base.Owner.Creature, null);
        }
    }
}