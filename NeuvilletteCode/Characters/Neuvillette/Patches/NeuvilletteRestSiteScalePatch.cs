using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
///     Patches NRestSiteCharacter.Create to set proper scale for Neuvillette's procedural rest site character
/// </summary>
[HarmonyPatch(typeof(NRestSiteCharacter), "Create")]
public static class NeuvilletteRestSiteScalePatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(Player player, int characterIndex, NRestSiteCharacter __result)
    {
        Logger.Info($"[RestSiteScalePatch] Called with player={player?.Character?.Id.Entry}, characterIndex={characterIndex}, result={__result?.Name}");

        if (__result == null || player == null)
        {
            Logger.Info("[RestSiteScalePatch] Early return: __result or player is null");
            return;
        }

        Logger.Info($"[RestSiteScalePatch] Player character type: {player.Character?.GetType().Name}");
        Logger.Info($"[RestSiteScalePatch] Is Neuvillette: {player.Character is Neuvillette}");
        
        if (player.Character is Neuvillette neuvillette)
        {
            Logger.Info($"[RestSiteScalePatch] WorldProceduralVisuals: {neuvillette.WorldProceduralVisuals}");
            Logger.Info($"[RestSiteScalePatch] RestSite: {neuvillette.WorldProceduralVisuals?.RestSite}");
            
            if (neuvillette.WorldProceduralVisuals?.RestSite != null)
            {
                __result.Scale = new Vector2(0.3f, 0.3f);
                __result.Position = new Vector2(66, -46);
                Logger.Info($"[RestSiteScalePatch] Applied scale={__result.Scale}, position={__result.Position}");
            }
            else
            {
                Logger.Info("[RestSiteScalePatch] WorldProceduralVisuals.RestSite is null, skipping");
            }
        }
    }
}

/// <summary>
///     Patches NRestSiteRoom._Ready to set proper position for Neuvillette's rest site character
///     (since NRestSiteRoom resets position to Vector2.Zero after creation)
/// </summary>
[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
public static class NeuvilletteRestSiteRoomPositionPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(NRestSiteRoom __instance)
    {
        Logger.Info("[RestSiteRoomPositionPatch] Called, adjusting Neuvillette character positions");
        
        foreach (var character in __instance.Characters)
        {
            if (character.Player?.Character is Neuvillette neuvillette && 
                neuvillette.WorldProceduralVisuals?.RestSite != null)
            {
                character.Position = new Vector2(66, -46);
                Logger.Info($"[RestSiteRoomPositionPatch] Adjusted position for {character.Name} to (66, -46)");
                
                AdjustThoughtBubblePositions(character);
            }
        }
    }
    
    private static void AdjustThoughtBubblePositions(NRestSiteCharacter character)
    {
        try
        {
            var controlRoot = character.GetNode<Control>("ControlRoot");
            var thoughtLeft = controlRoot.GetNode<Control>("%ThoughtBubbleLeft");
            var thoughtRight = controlRoot.GetNode<Control>("%ThoughtBubbleRight");
            
            float scaleFactor = 0.3f;
            
            thoughtLeft.OffsetLeft = -73.6836f * scaleFactor;
            thoughtLeft.OffsetTop = -324.997f * scaleFactor;
            thoughtRight.OffsetLeft = 209.209f * scaleFactor;
            thoughtRight.OffsetTop = -317.103f * scaleFactor;
            
            Logger.Info($"[RestSiteRoomPositionPatch] Adjusted thought bubble positions with scale factor {scaleFactor}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[RestSiteRoomPositionPatch] Failed to adjust thought bubble positions: {ex.Message}");
        }
    }
}

/// <summary>
///     Patches NRestSiteCharacter.FlipX to also flip the Sprite2D node for Neuvillette's procedural character
/// </summary>
[HarmonyPatch(typeof(NRestSiteCharacter), "FlipX")]
public static class NeuvilletteRestSiteFlipXPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (__instance?.Player?.Character is Neuvillette neuvillette && 
            neuvillette.WorldProceduralVisuals?.RestSite != null)
        {
            try
            {
                var visuals = __instance.GetNodeOrNull<Sprite2D>("Visuals");
                if (visuals != null)
                {
                    Vector2 scale = visuals.Scale;
                    scale.X = -scale.X;
                    visuals.Scale = scale;
                    
                    Vector2 position = visuals.Position;
                    position.X -= 43f;
                    position.Y -= 25f;
                    visuals.Position = position;
                    
                    Logger.Info($"[RestSiteFlipXPatch] Flipped Sprite2D for {__instance.Name}, new scale={visuals.Scale}, new position={visuals.Position}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[RestSiteFlipXPatch] Failed to flip Sprite2D: {ex.Message}");
            }
        }
    }
}