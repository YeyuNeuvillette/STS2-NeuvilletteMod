using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class PhantomPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldClearBlock(Creature creature)
    {
        if (creature != Owner) return true;
        return false;
    }

    public override async Task AfterBlockBroken(Creature creature)
    {
        if (creature == Owner)
        {
            var narwhal = creature.Monster as AllDevouringNarwhal;
            if (narwhal != null)
            {
                await narwhal.ExitBelly();
            }
        }
    }
}