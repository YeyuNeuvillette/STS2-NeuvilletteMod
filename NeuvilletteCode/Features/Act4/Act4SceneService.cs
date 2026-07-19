using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;
using Neuvillette.Infrastructure;
using Neuvillette.Api;

namespace Neuvillette.Features.Act4;

internal static class Act4SceneService
{
    private const string Act4BackgroundScenePath = "res://Neuvillette/scenes/ui/act4_bg.tscn";

    internal static bool TryCreateCombatBackground(BackgroundAssets assets, ref NCombatBackground __result)
    {
        var state = GameCompatibility.GetRunState();
        if (!GameCompatibility.IsNeuvilletteAct(state))
            return false;

        var contributedBackground = ApiRegistry.CreateAct4Background(assets);
        if (contributedBackground != null)
        {
            __result = contributedBackground;
            return true;
        }

        MainFile.Logger.Info("[Act4SceneService] Loading custom act4_bg.tscn...");
        var scene = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene(Act4BackgroundScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn("[Act4SceneService] Failed to load act4_bg.tscn");
            return false;
        }

        var combatBg = scene.Instantiate<NCombatBackground>(PackedScene.GenEditState.Disabled);
        if (combatBg == null)
        {
            MainFile.Logger.Warn("[Act4SceneService] Failed to instantiate act4_bg.tscn");
            return false;
        }

        __result = combatBg;
        return true;
    }
}
