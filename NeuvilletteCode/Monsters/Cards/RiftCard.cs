using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class RiftCard : BaseCard
{
    public override int MaxUpgradeLevel => 0;

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Unpowered | ValueProp.Move)
    ];

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

        await CreatureCmd.LoseMaxHp(choiceContext, creature, DynamicVars.Damage.IntValue, true);

        foreach (var enemy in combatState.Enemies)
        {
            if (enemy.Monster is AllDevouringNarwhal)
            {
                await CreatureCmd.GainMaxHp(enemy, DynamicVars.Damage.IntValue);
            }
        }
    }
}