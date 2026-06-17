using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Monsters.Afflictions;
using Neuvillette.Monsters.Cards;
using Neuvillette.Monsters.Powers;
using Neuvillette.Scripts;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace Neuvillette.Monsters;

[RegisterMonster]
public class AllDevouringNarwhal : ModMonsterTemplate
{
    private bool _isInBelly;
    private bool _triggered75;
    private bool _triggered25;
    private bool _pendingBellyEntry;
    private int _savedHp;
    private int _savedMaxHp;
    private new MonsterMoveStateMachine _moveStateMachine = null!;
    private List<PowerModel> _phaseOnePowers = new();
    private List<PowerModel> _phaseTwoPowers = new();
    private List<PowerModel>? _delayedPowersToReapply;

    private readonly Dictionary<CardModel, decimal> _devouredCards = new();
    private readonly HashSet<CardModel> _cravingExhaustedCards = new();

    private int AttackDamage1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 16);
    private int AttackDamage2 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 20);
    private int AttackDamage3 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 10);
    private int BellyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 160, 160);
    private int BellyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 42, 42);

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 999, 979);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://Neuvillette/scenes/monster/AllDevouringNarwhal.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await PowerCmd.Apply<BeastOfStarsPower>(new ThrowingPlayerChoiceContext(), Creature, 10m, Creature, null);
        await PowerCmd.Apply<DevourPower>(new ThrowingPlayerChoiceContext(), Creature, 25m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackWithAppetite = new MoveState(
            "ATTACK_WITH_APPETITE",
            AttackWithAppetiteMove,
            new SingleAttackIntent(AttackDamage1),
            new DebuffIntent()
        );

        var devourCraving = new MoveState(
            "DEVOUR_CRAVING",
            DevourCravingMove,
            new SingleAttackIntent(AttackDamage2),
            new StatusIntent(1)
        );

        var doubleAttack = new MoveState(
            "DOUBLE_ATTACK",
            DoubleAttackMove,
            new MultiAttackIntent(AttackDamage3, 2)
        );

        var bellyDefend = new MoveState(
            "BELLY_DEFEND",
            BellyDefendMove,
            new DefendIntent()
        );

        var bellyAttack = new MoveState(
            "BELLY_ATTACK",
            BellyAttackMove,
            new SingleAttackIntent(BellyDamage)
        );

        attackWithAppetite.FollowUpState = devourCraving;
        devourCraving.FollowUpState = doubleAttack;
        doubleAttack.FollowUpState = devourCraving;

        bellyDefend.MustPerformOnceBeforeTransitioning = true;
        bellyDefend.FollowUpState = bellyAttack;
        bellyAttack.FollowUpState = bellyAttack;

        _moveStateMachine = new MonsterMoveStateMachine(
            [attackWithAppetite, devourCraving, doubleAttack, bellyDefend, bellyAttack],
            attackWithAppetite
        );

        return _moveStateMachine;
    }

    private async Task AttackWithAppetiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage1)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<AppetitePower>(new ThrowingPlayerChoiceContext(), target, 3m, Creature, null);
        }
    }

    private async Task DevourCravingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage2)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var target in targets)
        {
            var player = target.Player ?? target.PetOwner;
            if (player == null) continue;

            var combatState = player.PlayerCombatState;
            if (combatState == null) continue;

            var handCards = PileType.Hand.GetPile(player).Cards;
            var drawCards = PileType.Draw.GetPile(player).Cards;
            var discardCards = PileType.Discard.GetPile(player).Cards;
            var allCards = handCards.Concat(drawCards).Concat(discardCards).ToList();
            var cravingCards = allCards.Where(c => c.Affliction is CravingAffliction).ToList();
            if (cravingCards.Count == 0) continue;

            var card = base.RunRng.CombatCardGeneration.NextItem(cravingCards);
            if (card == null) continue;
            var currentPile = card.Pile?.Type ?? PileType.Hand;

            _cravingExhaustedCards.Add(card);
            CardCmd.ClearAffliction(card);
            await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), card);
            var riftCard = card.CardScope?.CreateCard<RiftCard>(player);
            if (riftCard == null) continue;
            await CardPileCmd.AddGeneratedCardToCombat(riftCard, currentPile, player);
        }
    }

    private async Task DoubleAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage3)
            .WithHitCount(2)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task BellyDefendMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BellyBlock, ValueProp.Move, null);
    }

    private async Task BellyAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BellyDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Creature || _isInBelly) return;

        float hpPercent = (float)Creature.CurrentHp / Creature.MaxHp;

        if ((!_triggered75 && hpPercent <= 0.75f) || (!_triggered25 && hpPercent <= 0.25f))
        {
            if (!_triggered75 && hpPercent <= 0.75f)
                _triggered75 = true;
            if (!_triggered25 && hpPercent <= 0.25f)
                _triggered25 = true;

            _pendingBellyEntry = true;
            var player = Creature.CombatState?.Players.FirstOrDefault();
            if (player != null)
            {
                PlayerCmd.EndTurn(player, canBackOut: false);
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && _pendingBellyEntry)
        {
            _pendingBellyEntry = false;
            await EnterBelly();
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Enemy && _delayedPowersToReapply != null && !Creature.IsStunned)
        {
            foreach (var power in _delayedPowersToReapply)
            {
                await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), power, Creature, power.Amount, power.Applier, null);
            }
            _delayedPowersToReapply = null;
        }
        await base.AfterSideTurnStart(side, participants, combatState);
    }

    private async Task EnterBelly()
    {
        _isInBelly = true;
        _savedHp = Creature.CurrentHp;
        _savedMaxHp = Creature.MaxHp;

        _phaseOnePowers = Creature.Powers.ToList();
        foreach (var power in _phaseOnePowers.ToList())
        {
            await PowerCmd.Remove(power);
        }

        await CreatureCmd.SetMaxAndCurrentHp(Creature, 999999999m);
        Creature.HpDisplay = HpDisplay.InfiniteWithoutNumbers;

        await PowerCmd.Apply<BarricadePower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
        await PowerCmd.Apply<PhantomPower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);

        _phaseTwoPowers = Creature.Powers.ToList();

        UpdateVisuals(true);
        SetMoveImmediate(GetBellyDefendState(), forceTransition: true);
    }

    public async Task ExitBelly()
    {
        _isInBelly = false;

        _phaseTwoPowers = Creature.Powers.ToList();
        foreach (var power in _phaseTwoPowers.ToList())
        {
            await PowerCmd.Remove(power);
        }

        var delayedPowers = _phaseOnePowers
            .Where(p => p is BeastOfStarsPower || p is DevourPower)
            .ToList();
        var immediatePowers = _phaseOnePowers
            .Where(p => p is not BeastOfStarsPower && p is not DevourPower)
            .ToList();

        foreach (var power in immediatePowers)
        {
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), power, Creature, power.Amount, power.Applier, null);
        }
        _phaseOnePowers.Clear();

        _delayedPowersToReapply = delayedPowers;

        Creature.HpDisplay = HpDisplay.Normal;
        Creature.SetMaxHpInternal((decimal)_savedMaxHp);
        Creature.SetCurrentHpInternal((decimal)_savedHp);

        UpdateVisuals(false);

        var player = Creature.CombatState?.Players.FirstOrDefault();
        if (player != null)
        {
            var playerDebuffs = player.Creature.Powers
                .Where(p => p.Type == PowerType.Debuff)
                .ToList();
            foreach (var debuff in playerDebuffs)
            {
                await PowerCmd.Remove(debuff);
            }

            await CreatureCmd.Heal(player.Creature, player.Creature.MaxHp);

            var combatState = player.PlayerCombatState;
            if (combatState != null)
            {
                var allCards = combatState.AllCards.ToList();

                var riftCards = allCards.Where(c => c is RiftCard).ToList();
                foreach (var rift in riftCards)
                {
                    await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), rift);
                }

                var cravingCards = allCards.Where(c => c.Affliction is CravingAffliction).ToList();
                foreach (var card in cravingCards)
                {
                    CardCmd.ClearAffliction(card);
                }

                var exhaustPile = PileType.Exhaust.GetPile(player);
                var cravingExhausted = exhaustPile.Cards
                    .Where(c => _cravingExhaustedCards.Contains(c))
                    .ToList();
                foreach (var card in cravingExhausted)
                {
                    await CardPileCmd.Add(card, PileType.Draw);
                }
                if (cravingExhausted.Count > 0)
                {
                    await CardPileCmd.Shuffle(new ThrowingPlayerChoiceContext(), player);
                }
                _cravingExhaustedCards.Clear();
            }
        }

        foreach (var kvp in _devouredCards)
        {
            var card = kvp.Key;
            if (card.DynamicVars?.TryGetValue("Damage", out var damageVar) == true)
            {
                damageVar.BaseValue = kvp.Value;
            }
        }
        _devouredCards.Clear();

        await CreatureCmd.Stun(Creature, "DEVOUR_CRAVING");
    }

    public void RecordDevouredCard(CardModel card, decimal originalBaseDamage)
    {
        if (!_devouredCards.ContainsKey(card))
        {
            _devouredCards[card] = originalBaseDamage;
        }
    }

    public void RecordCravingExhaustedCard(CardModel card)
    {
        _cravingExhaustedCards.Add(card);
    }

    private MoveState GetBellyDefendState()
    {
        return (MoveState)_moveStateMachine.States["BELLY_DEFEND"];
    }

    private void UpdateVisuals(bool isInBelly)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode?.Visuals is NarwhalCreatureVisuals visuals)
        {
            visuals.SetBellyState(isInBelly);
        }
    }
}