using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Narcissus : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShowCounter => true;

    public override int DisplayAmount => HpLossCount;

    private int _hpLossCount;

    [SavedProperty]
    public int HpLossCount
    {
        get
        {
            return _hpLossCount;
        }
        set
        {
            _hpLossCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart()
    {
        HpLossCount = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner?.Creature)
            return;

        if (!CombatManager.Instance.IsInProgress)
            return;

        if (delta >= 0)
            return;

        HpLossCount++;
        if (HpLossCount >= 2)
        {
            HpLossCount = 0;
            Flash();
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), Owner);
        }
    }
}