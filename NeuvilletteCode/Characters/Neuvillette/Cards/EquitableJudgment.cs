using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
[RegisterCharacterStarterCard(typeof(Neuvillette))]
public sealed class EquitableJudgment() : NeuvilletteCard(3, CardType.Attack, CardRarity.Basic, TargetType.AllEnemies)
{
    private const string CalculatedHpLossKey = "CalculatedHpLoss";

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.SourcewaterDroplet
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(2m),
        new CalculationBaseVar(15m),
        new ExtraDamageVar(15m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (CardModel card, Creature? _) =>
            GetPastDraconicGloriesPower(card)?.Amount ?? 0m),
        new JudgmentHpLossVar()
    ];

    private static PastDraconicGloriesPower? GetPastDraconicGloriesPower(CardModel card)
    {
        if (!card.IsMutable || card.Owner == null || card.Pile == null || !card.Pile.IsCombatPile)
        {
            return null;
        }
        return card.Owner.Creature.GetPower<PastDraconicGloriesPower>();
    }

    private static decimal GetAdditionalHpLoss(CardModel card)
    {
        var power = GetPastDraconicGloriesPower(card);
        return power != null ? 2m * power.Amount : 0m;
    }

    private sealed class JudgmentHpLossVar : DynamicVar
    {
        public JudgmentHpLossVar() : base(CalculatedHpLossKey, 2m)
        {
        }

        protected override decimal GetBaseValueForIConvertible()
        {
            return _owner is EquitableJudgment card
                ? card.DynamicVars.HpLoss.BaseValue + GetAdditionalHpLoss(card)
                : BaseValue;
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            if (_owner is EquitableJudgment)
            {
                base.PreviewValue = card.DynamicVars.HpLoss.BaseValue + GetAdditionalHpLoss(card);
            }
        }
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card != this)
        {
            return base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);
        }

        var isModified = base.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);
        if (!isModified)
        {
            modifiedCost = originalCost;
        }

        var dropletCount = card.Owner?.Creature?.GetPower<SourcewaterDroplet>()?.Amount ?? 0;
        if (dropletCount <= 0)
        {
            return isModified;
        }

        modifiedCost = Math.Max(0m, modifiedCost - dropletCount);
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;

        var dropletPower = creature.GetPower<SourcewaterDroplet>();
        if (dropletPower != null)
        {
            await PowerCmd.Remove(dropletPower);
        }

        if (CombatState != null && RunState != null)
        {
            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();

            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithAttackerAnim("Cast", 0.5f)
                .BeforeDamage(async () =>
                {
                    if (enemies.Count > 0)
                    {
                        var beamVfx = NHyperbeamVfx.Create(creature, enemies[^1]);
                        if (beamVfx != null)
                        {
                            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(beamVfx);
                            await Cmd.Wait(0.5f);
                        }
                        foreach (var enemy in enemies)
                        {
                            var impactVfx = NHyperbeamImpactVfx.Create(creature, enemy);
                            if (impactVfx != null)
                            {
                                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(impactVfx);
                            }
                        }
                    }
                })
                .Execute(choiceContext);
        }

        var hpLoss = DynamicVars.HpLoss.BaseValue + GetAdditionalHpLoss(this);
        await CreatureCmd.Damage(
            choiceContext,
            creature,
            hpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(5m);
        DynamicVars.ExtraDamage.UpgradeValueBy(5m);
    }
}