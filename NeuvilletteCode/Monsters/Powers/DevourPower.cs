using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class DevourPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (cardSource == null || cardSource.Type != CardType.Attack) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0) return;

        int vigorAmount = (int)(result.UnblockedDamage * Amount / 100m);
        if (vigorAmount > 0)
        {
            Flash();
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner, vigorAmount, Owner, null);

            if (cardSource.DynamicVars.TryGetValue("Damage", out var damageVar))
            {
                var narwhal = Owner.Monster as AllDevouringNarwhal;
                narwhal?.RecordDevouredCard(cardSource, damageVar.BaseValue);

                decimal newBase = damageVar.BaseValue - vigorAmount;
                if (newBase < 0m) newBase = 0m;
                damageVar.BaseValue = newBase;
            }
        }
    }
}