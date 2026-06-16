using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class RiftCard : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Unpowered | ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];

    public override bool HasTurnEndInHandEffect => true;

    public RiftCard()
        : base(1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var creature = Owner?.Creature;
        if (creature == null) return;

        var combatState = creature.CombatState;
        if (combatState == null) return;

        var enemies = combatState.Enemies;
        foreach (var enemy in enemies)
        {
            if (enemy.Monster is AllDevouringNarwhal)
            {
                await CreatureCmd.LoseMaxHp(choiceContext, creature, DynamicVars.Damage.IntValue, false);
                return;
            }
        }
    }
}