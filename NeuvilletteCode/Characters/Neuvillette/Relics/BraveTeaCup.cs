using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class BraveTeaCup : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BattleCount;

    private int _battleCount;

    [SavedProperty]
    public int BattleCount
    {
        get
        {
            return _battleCount;
        }
        set
        {
            _battleCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);

        if (Owner?.Creature == null)
            return;

        BattleCount++;
        if (BattleCount >= 2)
        {
            BattleCount = 0;
            Flash();
            await CreatureCmd.GainMaxHp(Owner.Creature, 3m);
        }
    }
}