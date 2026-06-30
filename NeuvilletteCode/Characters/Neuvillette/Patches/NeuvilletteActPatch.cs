using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using Neuvillette.Characters.Neuvillette.Ancients;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Monsters;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
public static class NeuvilletteActPatch
{
    [HarmonyPatch(typeof(StandardActMap), "AssignPointTypes")]
    [HarmonyPostfix]
    public static void Postfix_Map(StandardActMap __instance)
    {
        var rm = RunManager.Instance;
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(rm) as RunState;

        if (state != null && state.Act is NeuvilletteAct)
        {
            var grid = AccessTools.Property(typeof(StandardActMap), "Grid").GetValue(__instance) as MapPoint[,];
            if (grid == null) return;

            for (int r = 1; r < grid.GetLength(1); r++)
                for (int c = 0; c < 7; c++) grid[c, r] = null!;

            SetPoint(grid, 3, 1, MapPointType.RestSite);
            SetPoint(grid, 3, 2, MapPointType.Shop);
            SetPoint(grid, 3, 3, MapPointType.Treasure);
            SetPoint(grid, 3, 4, MapPointType.RestSite);

            __instance.StartingMapPoint.PointType = MapPointType.Ancient;
            __instance.BossMapPoint.PointType = MapPointType.Boss;

            __instance.StartingMapPoint.Children.Clear();
            __instance.StartingMapPoint.AddChildPoint(grid[3, 1]);

            grid[3, 1].Children.Clear(); grid[3, 1].AddChildPoint(grid[3, 2]);
            grid[3, 2].Children.Clear(); grid[3, 2].AddChildPoint(grid[3, 3]);
            grid[3, 3].Children.Clear(); grid[3, 3].AddChildPoint(grid[3, 4]);

            grid[3, 4].Children.Clear(); grid[3, 4].AddChildPoint(__instance.BossMapPoint);
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_GenerateRooms(RunManager __instance)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null || state.Acts.Count <= 3) return;

        if (__instance.AscensionManager.HasLevel(AscensionLevel.DoubleBoss))
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

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_Rooms(ActModel __instance)
    {
        if (__instance is NeuvilletteAct)
        {
            var rooms = AccessTools.Field(typeof(ActModel), "_rooms").GetValue(__instance) as RoomSet;
            if (rooms != null)
            {
                rooms.Boss = ModelDb.Encounter<NarwhalBossEncounter>();
                rooms.Ancient = ModelDb.AncientEvent<ArchitectAncient>();
                rooms.eliteEncounters.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(NRewardsScreen), "ShowScreen")]
    [HarmonyPrefix]
    public static bool Prefix_ShowScreen(RewardsSet set, bool isTerminal, IRunState runState)
    {
        if (!isTerminal || runState.CurrentRoom.RoomType != RoomType.Boss) return true;
        if (runState.CurrentActIndex != 2) return true;

        if (runState.Map.SecondBossMapPoint != null
            && runState.CurrentMapCoord == runState.Map.BossMapPoint.coord)
        {
            TaskHelper.RunSafely(RunManager.Instance.ProceedFromTerminalRewardsScreen());
            return false;
        }

        RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
        return false;
    }

    [HarmonyPatch(typeof(NCombatBackground), nameof(NCombatBackground.Create))]
    [HarmonyPrefix]
    public static bool Prefix_CreateCombatBg(BackgroundAssets bg, ref NCombatBackground __result)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) as RunState;
        MainFile.Logger.Info($"[Neuvillette Act] Prefix_CreateCombatBg called, act={state?.Act?.GetType().Name ?? "null"}");
        if (state != null && state.Act is NeuvilletteAct)
        {
            MainFile.Logger.Info("[Neuvillette Act] Loading custom act4_bg.tscn...");
            var scene = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene("res://Neuvillette/scenes/ui/act4_bg.tscn");
            MainFile.Logger.Info($"[Neuvillette Act] Scene loaded: {scene != null}, instantiating...");
            var combatBg = scene!.Instantiate<NCombatBackground>(PackedScene.GenEditState.Disabled);
            MainFile.Logger.Info($"[Neuvillette Act] Instantiate result: {combatBg != null}");
            __result = combatBg!;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(NRunMusicController), "UpdateMusic")]
    [HarmonyPrefix]
    public static bool Prefix_Music()
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) as RunState;
        if (state != null && state.Act is NeuvilletteAct) return false;
        return true;
    }

    [HarmonyPatch(typeof(TreasureRoom), MethodType.Constructor, typeof(int))]
    [HarmonyPrefix]
    public static void Prefix_TreasureRoom(ref int actIndex)
    {
        if (actIndex > 2) actIndex = 2;
    }

    private static int _restoredActIndex = -1;

    [HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
    [HarmonyPrefix]
    public static void Prefix_RestSiteReady(NRestSiteCharacter __instance)
    {
        var runState = __instance.Player?.RunState;
        if (runState != null && runState.CurrentActIndex > 2)
        {
            _restoredActIndex = runState.CurrentActIndex;
            AccessTools.Field(typeof(RunState), "_currentActIndex").SetValue(runState, 2);
        }
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
    [HarmonyPostfix]
    public static void Postfix_RestSiteReady(NRestSiteCharacter __instance)
    {
        if (_restoredActIndex > 2)
        {
            var runState = __instance.Player?.RunState;
            if (runState != null)
            {
                AccessTools.Field(typeof(RunState), "_currentActIndex").SetValue(runState, _restoredActIndex);
            }
            _restoredActIndex = -1;
        }
    }

    [HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.GetMultiplayerScaling))]
    [HarmonyPrefix]
    public static bool Prefix_GetMultiplayerScaling(EncounterModel? encounter, ref int actIndex, ref decimal __result)
    {
        if (actIndex > 2)
        {
            actIndex = 2;
        }
        return true;
    }

    private static void SetPoint(MapPoint[,] grid, int col, int row, MapPointType type)
    {
        MapPoint p = new MapPoint(col, row);
        p.PointType = type;
        p.CanBeModified = false;
        grid[col, row] = p;
    }
}