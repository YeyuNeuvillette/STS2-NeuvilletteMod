using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using Neuvillette.Monsters;

namespace Neuvillette.Characters.Neuvillette.Act;

public sealed class NeuvilletteAct : ActModel
{
    protected override int BaseNumberOfRooms => 7;

    public override int Index => -1;
    public override bool IsDefault => false;
    public override bool IsUnlocked(UnlockState unlockState) => true;

    public override string[] BgMusicOptions => new string[] { "event:/music/act3_boss_queen", "event:/music/act3_boss_queen" };
    public override string[] MusicBankPaths => new string[] { "res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank" };
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

    public override Color MapBgColor => new Color("819A97");
    public override Color MapTraveledColor => new Color("1D1E2F");
    public override Color MapUntraveledColor => new Color("60717C");

    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new List<EncounterModel>
        {
            ModelDb.Encounter<NarwhalBossEncounter>()
        };
    }

    public override IEnumerable<AncientEventModel> AllAncients
    {
        get
        {
            return new List<AncientEventModel>
            {
                ModelDb.AncientEvent<Nonupeipe>(),
                ModelDb.AncientEvent<Tanx>(),
                ModelDb.AncientEvent<Vakuu>()
            };
        }
    }

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState state) => AllAncients;

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new List<EncounterModel> { ModelDb.Encounter<NarwhalBossEncounter>() };

    public override IEnumerable<EventModel> AllEvents => new List<EventModel>();

    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextInt(1, 2);
        int unknownCount = 0;
        return new MapPointTypeCounts(unknownCount, restCount);
    }
}