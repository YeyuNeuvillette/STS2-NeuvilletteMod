using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Neuvillette.Characters.Neuvillette.Cards;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(SlipperyBridge), "GenerateInitialOptions")]
public static class SlipperyBridgePatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(SlipperyBridge __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        if (__instance.Owner.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
        {
            return;
        }

        var mutable = __result as List<EventOption> ?? __result.ToList();
        mutable.Add(new EventOption(__instance, () => CallDownpour(__instance), "SLIPPERY_BRIDGE.pages.INITIAL.options.CALL_DOWNPOUR", HoverTipFactory.FromCardWithCardHoverTips<Downpour>()));
        __result = mutable;
    }

    private static async Task CallDownpour(SlipperyBridge @event)
    {
        if (@event.Owner == null)
            return;

        var card = @event.Owner.RunState.CreateCard<Downpour>(@event.Owner);
        if (card != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
        }
        
        var vfxContainer = NEventRoom.Instance?.VfxContainer;
        if (vfxContainer != null)
        {
            foreach (var child in vfxContainer.GetChildren())
            {
                if (child is NRainVfx rainVfx)
                {
                    rainVfx.Emitting = false;
                    break;
                }
            }
        }
        
        @event.SetEventFinished(@event.L10NLookup("SLIPPERY_BRIDGE.pages.CALL_DOWNPOUR.description"));
    }
}