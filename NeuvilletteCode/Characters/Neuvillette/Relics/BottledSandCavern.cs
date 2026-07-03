using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class BottledSandCavern : BaseRelic
{
    private int _progressCounter;
    private bool _killTriggered;

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool HasUponPickupEffect => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => _progressCounter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<SandVortex>());

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, 6)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs, c => CreateSandVortexTransformation(c, forPreview: true));
        var transformations = selectedCards.Select(c => CreateSandVortexTransformation(c, forPreview: false)).ToList();

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, Owner.PlayerRng.Transformations);
        }
    }

    private CardTransformation CreateSandVortexTransformation(CardModel original, bool forPreview)
    {
        var sandVortex = forPreview
            ? ModelDb.Card<SandVortex>().ToMutable()
            : Owner.RunState.CreateCard<SandVortex>(Owner);
        return new CardTransformation(original, sandVortex);
    }

    public override Task BeforeCombatStart()
    {
        _progressCounter = 0;
        _killTriggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);

        if (player != Owner)
            return;

        _progressCounter++;
        InvokeDisplayAmountChanged();

        if (_progressCounter < 35 || _killTriggered)
            return;

        var creature = Owner?.Creature;
        if (creature == null)
            return;

        _killTriggered = true;
        Flash();

        var enemies = creature.CombatState?.Enemies.Where(e => !e.IsDead).ToList();
        if (enemies is { Count: > 0 })
            await CreatureCmd.Kill(enemies);
    }

    public async Task IncrementProgressCounter()
    {
        _progressCounter++;
        InvokeDisplayAmountChanged();

        if (_progressCounter < 35 || _killTriggered)
            return;

        var creature = Owner?.Creature;
        if (creature == null)
            return;

        _killTriggered = true;
        Flash();

        var enemies = creature.CombatState?.Enemies.Where(e => !e.IsDead).ToList();
        if (enemies is { Count: > 0 })
            await CreatureCmd.Kill(enemies);
    }
}