using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Api;

namespace Neuvillette.Features.Act4;

internal static class Act4RewardService
{
    internal static bool TryHandleBossRewardScreen(RewardsSet set, bool isTerminal, IRunState runState)
    {
        if (!isTerminal || runState.CurrentRoom?.RoomType != RoomType.Boss)
            return false;

        if (runState.CurrentActIndex != 2)
            return false;

        if (ApiRegistry.TryHandleAct4Rewards(set, runState))
            return true;

        if (runState.Map.SecondBossMapPoint != null
            && runState.CurrentMapCoord == runState.Map.BossMapPoint.coord)
        {
            TaskHelper.RunSafely(RunManager.Instance.ProceedFromTerminalRewardsScreen());
            return true;
        }

        RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
        return true;
    }
}
