using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Hooks;
using Neuvillette.Characters.Neuvillette.Powers;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
[RegisterCharacterStarterCard(typeof(Neuvillette))]
public sealed class EquitableJudgment() : NeuvilletteCard(3, CardType.Attack, CardRarity.Basic, TargetType.AllEnemies)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.SourcewaterDroplet
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(2m),
        new DamageVar(15m, ValueProp.Move),
        new CalculatedDamageVar(ValueProp.Move),
        new CalculatedHpLossVar()
    ];

    private class CalculatedDamageVar : DynamicVar
    {
        public CalculatedDamageVar(ValueProp props) : base("CalculatedDamage", 15m)
        {
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            if (_owner is EquitableJudgment card)
            {
                var power = GetPower(card);
                var multiplier = power != null ? 1m + power.Amount : 1m;
                
                if (card.RunState == null || card.CombatState == null)
                {
                    return card.DynamicVars.Damage.BaseValue * multiplier;
                }
                
                var modifiedDamage = Hook.ModifyDamage(
                    card.RunState,
                    card.CombatState,
                    null,
                    card.Owner?.Creature,
                    card.DynamicVars.Damage.BaseValue,
                    card.DynamicVars.Damage.Props,
                    card,
                    ModifyDamageHookType.All,
                    CardPreviewMode.Normal,
                    out IEnumerable<AbstractModel> _);
                
                return modifiedDamage * multiplier;
            }
            return BaseValue;
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            if (_owner is EquitableJudgment equitableJudgment)
            {
                var power = GetPower(equitableJudgment);
                var multiplier = power != null ? 1m + power.Amount : 1m;
                
                if (equitableJudgment.RunState == null || equitableJudgment.CombatState == null)
                {
                    base.PreviewValue = equitableJudgment.DynamicVars.Damage.BaseValue * multiplier;
                    return;
                }
                
                var modifiedDamage = Hook.ModifyDamage(
                    equitableJudgment.RunState,
                    equitableJudgment.CombatState,
                    target,
                    equitableJudgment.Owner?.Creature,
                    equitableJudgment.DynamicVars.Damage.BaseValue,
                    equitableJudgment.DynamicVars.Damage.Props,
                    equitableJudgment,
                    ModifyDamageHookType.All,
                    previewMode,
                    out IEnumerable<AbstractModel> _);
                
                base.PreviewValue = modifiedDamage * multiplier;
            }
        }

        private static PastDraconicGloriesPower? GetPower(CardModel card)
        {
            if (!card.IsMutable)
            {
                return null;
            }
            if (card.Owner == null)
            {
                return null;
            }
            if (card.Pile == null)
            {
                return null;
            }
            if (!card.Pile.IsCombatPile)
            {
                return null;
            }
            return card.Owner.Creature.GetPower<PastDraconicGloriesPower>();
        }
    }

    private class CalculatedHpLossVar : DynamicVar
    {
        public CalculatedHpLossVar() : base("CalculatedHpLoss", 2m)
        {
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            if (_owner is EquitableJudgment card)
            {
                var power = GetPower(card);
                var additionalLoss = power != null ? 2m * power.Amount : 0m;
                return card.DynamicVars.HpLoss.BaseValue + additionalLoss;
            }
            return BaseValue;
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            if (_owner is EquitableJudgment equitableJudgment)
            {
                var power = GetPower(equitableJudgment);
                var additionalLoss = power != null ? 2m * power.Amount : 0m;
                base.PreviewValue = equitableJudgment.DynamicVars.HpLoss.BaseValue + additionalLoss;
            }
        }

        private static PastDraconicGloriesPower? GetPower(CardModel card)
        {
            if (!card.IsMutable)
            {
                return null;
            }
            if (card.Owner == null)
            {
                return null;
            }
            if (card.Pile == null)
            {
                return null;
            }
            if (!card.Pile.IsCombatPile)
            {
                return null;
            }
            return card.Owner.Creature.GetPower<PastDraconicGloriesPower>();
        }
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card != this)
            return base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);

        var isModified = base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);
        if (!isModified)
            modifiedCost = originalCost;

        var dropletPower = Owner?.Creature?.GetPower<Powers.SourcewaterDroplet>();
        var dropletCount = dropletPower?.Amount ?? 0;
        if (dropletCount <= 0)
            return isModified;

        modifiedCost = Math.Max(0m, modifiedCost - dropletCount);
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var dropletPower = Owner.Creature.GetPower<Powers.SourcewaterDroplet>();
        if (dropletPower != null)
            await PowerCmd.Remove(dropletPower);

        var pastDraconicGloriesPower = Owner.Creature.GetPower<PastDraconicGloriesPower>();
        var multiplier = pastDraconicGloriesPower != null ? 1m + pastDraconicGloriesPower.Amount : 1m;

        if (CombatState != null && RunState != null)
        {
            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
            
            var modifiedDamage = Hook.ModifyDamage(
                RunState,
                CombatState,
                null,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                DynamicVars.Damage.Props,
                this,
                ModifyDamageHookType.All,
                CardPreviewMode.None,
                out IEnumerable<AbstractModel> _);
            
            await DamageCmd.Attack(modifiedDamage * multiplier)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithAttackerAnim("Cast", 0.5f)
                .BeforeDamage(async delegate
                {
                    NHyperbeamVfx? nHyperbeamVfx = NHyperbeamVfx.Create(Owner.Creature, enemies.Last());
                    if (nHyperbeamVfx != null)
                    {
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamVfx);
                        await Cmd.Wait(0.5f);
                    }
                    foreach (Creature item in enemies)
                    {
                        NHyperbeamImpactVfx? nHyperbeamImpactVfx = NHyperbeamImpactVfx.Create(Owner.Creature, item);
                        if (nHyperbeamImpactVfx != null)
                        {
                            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamImpactVfx);
                        }
                    }
                })
                .Execute(choiceContext);
        }

        var additionalHpLoss = pastDraconicGloriesPower != null ? 2m * pastDraconicGloriesPower.Amount : 0m;
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue + additionalHpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        
        if (DynamicVars.TryGetValue("CalculatedDamage", out var calculatedDamage))
        {
            calculatedDamage.BaseValue += 5m;
        }
    }
}