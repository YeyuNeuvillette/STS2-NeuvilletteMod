using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Ancients;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Infrastructure;
using Neuvillette.Monsters;
using Neuvillette.Api;

namespace Neuvillette.Features.Act4;

internal static class Act4RoomService
{
    internal static void ConfigureDoubleBoss(RunManager runManager, RunState state)
    {
        if (state.Acts.Count <= 3)
            return;

        if (runManager.AscensionManager.HasLevel(AscensionLevel.DoubleBoss))
        {
            var gloryAct = state.Acts[2];
            if (!gloryAct.HasSecondBoss)
            {
                var secondBoss = state.Rng.UpFront.NextItem(
                    gloryAct.AllBossEncounters.Where(e => e.Id != gloryAct.BossEncounter.Id));
                gloryAct.SetSecondBossEncounter(secondBoss);
            }
        }
    }

    internal static bool TryConfigureNeuvilletteRooms(ActModel act)
    {
        if (act is not NeuvilletteAct)
            return false;

        var rooms = GameCompatibility.GetRooms(act);
        if (rooms == null)
            return false;

        rooms.Boss = ModelDb.Encounter<NarwhalBossEncounter>();
        rooms.Ancient = ModelDb.AncientEvent<ArchitectAncient>();
        rooms.eliteEncounters.Clear();
        ApiRegistry.ConfigureAct4Rooms(act, rooms);
        return true;
    }
}
