using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Monsters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
public static class NeuvilletteActPatch
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetActInternal))]
    [HarmonyPostfix]
    public static void Postfix_SyncAct(RunManager __instance, int actIndex)
    {
        if (actIndex == 3)
        {
            var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
            if (state == null) return;

            var syncField = AccessTools.GetDeclaredFields(typeof(RunManager))
                .FirstOrDefault(f => f.FieldType == typeof(MapSelectionSynchronizer));

            if (syncField != null)
            {
                var synchronizer = syncField.GetValue(__instance) as MapSelectionSynchronizer;
                if (synchronizer != null)
                {
                    AccessTools.Method(typeof(MapSelectionSynchronizer), "BeforeMapGenerated")?.Invoke(synchronizer, null);
                    MainFile.Logger.Info("[Neuvillette Act] 同步器状态已强制更新至第四幕。");
                }
            }
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
    [HarmonyPrefix]
    public static bool Prefix_Sequence(RunManager __instance, ref Task __result)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return true;

        if (state.CurrentActIndex == 2)
        {
            var acts = state.Acts.ToList();
            if (acts.Count == 3)
            {
                var finalAct = ModelDb.Act<NeuvilletteAct>().ToMutable();
                finalAct.GenerateRooms(state.Rng.UpFront, state.UnlockState, state.Players.Count > 1);

                acts.Add(finalAct);
                AccessTools.Property(typeof(RunState), "Acts").SetValue(state, acts);
                MainFile.Logger.Info("[Neuvillette Act] 已在关卡序列末尾成功注入第四层。");
            }
            return true;
        }
        return true;
    }

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
                rooms.eliteEncounters.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(ActModel), "get_MapTopBgPath")]
    [HarmonyPrefix]
    public static bool Prefix_MapTopBgPath(ActModel __instance, ref string __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = "res://images/packed/map/map_bgs/glory/map_top_glory.png";
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapMidBgPath")]
    [HarmonyPrefix]
    public static bool Prefix_MapMidBgPath(ActModel __instance, ref string __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = "res://images/packed/map/map_bgs/glory/map_middle_glory.png";
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapBotBgPath")]
    [HarmonyPrefix]
    public static bool Prefix_MapBotBgPath(ActModel __instance, ref string __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = "res://images/packed/map/map_bgs/glory/map_bottom_glory.png";
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapTopBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapTopBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_top_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapMidBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapMidBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_middle_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapBotBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapBotBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_bottom_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_RestSiteBackgroundPath")]
    [HarmonyPrefix]
    public static bool Prefix_Rest(ActModel __instance, ref string __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = "res://scenes/rest_site/glory_rest_site.tscn";
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_BackgroundScenePath")]
    [HarmonyPrefix]
    public static bool Prefix_Bg(ActModel __instance, ref string __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = "res://scenes/backgrounds/glory/glory_background.tscn";
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    [HarmonyPrefix]
    public static bool Prefix_Assets(ActModel __instance, MegaCrit.Sts2.Core.Random.Rng rng, ref BackgroundAssets __result)
    {
        if (__instance is NeuvilletteAct)
        {
            __result = new BackgroundAssets("glory", rng);
            return false;
        }
        return true;
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

    private static void SetPoint(MapPoint[,] grid, int col, int row, MapPointType type)
    {
        MapPoint p = new MapPoint(col, row);
        p.PointType = type;
        p.CanBeModified = false;
        grid[col, row] = p;
    }
}