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
using MegaCrit.Sts2.Core.Nodes.Audio;
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
    private int _savedHp;
    private int _savedMaxHp;
    private int _phaseOneLoopCount;
    private new MonsterMoveStateMachine _moveStateMachine = null!;
    private MoveState _devourCravingState = null!;
    private MoveState _doubleAttackState = null!;
    private MoveState _empowerAttackState = null!;
    private MoveState _enterBellyState = null!;
    private List<PowerModel> _phaseOnePowers = new();
    private List<PowerModel> _phaseTwoPowers = new();
    private List<PowerModel>? _delayedPowersToReapply;

    private readonly Dictionary<CardModel, decimal> _devouredCards = new();
    private readonly HashSet<CardModel> _cravingExhaustedCards = new();
    private readonly Dictionary<Player, CardModel> _lastPlayedCravingCard = new();

    private int AttackDamage1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 16);
    private int AttackDamage2 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 20);
    private int AttackDamage3 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 10);
    private int EmpowerDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 15);
    private int EmpowerStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 5);
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
        foreach (var opponent in Creature.CombatState!.GetOpponentsOf(Creature))
        {
            var beastPower = (BeastOfStarsPower)ModelDb.Power<BeastOfStarsPower>().ToMutable();
            beastPower.Target = opponent;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), beastPower, Creature, 10m, Creature, null);
        }
        foreach (var opponent in Creature.CombatState!.GetOpponentsOf(Creature))
        {
            var devourPower = (DevourPower)ModelDb.Power<DevourPower>().ToMutable();
            devourPower.Target = opponent;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), devourPower, Creature, 1m, Creature, null);
        }
        await PowerCmd.Apply<HostilityPower>(new ThrowingPlayerChoiceContext(), Creature, 75m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attackWithAppetite = new MoveState(
            "ATTACK_WITH_APPETITE",
            AttackWithAppetiteMove,
            new SingleAttackIntent(AttackDamage1),
            new DebuffIntent()
        );

        _devourCravingState = new MoveState(
            "DEVOUR_CRAVING",
            DevourCravingMove,
            new SingleAttackIntent(AttackDamage2),
            new StatusIntent(1)
        );

        _doubleAttackState = new MoveState(
            "DOUBLE_ATTACK",
            DoubleAttackMove,
            new MultiAttackIntent(AttackDamage3, 2)
        );

        _empowerAttackState = new MoveState(
            "EMPOWER_ATTACK",
            EmpowerAttackMove,
            new SingleAttackIntent(EmpowerDamage),
            new BuffIntent()
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

        _enterBellyState = new MoveState(
            "ENTER_BELLY",
            EnterBellyMove,
            new StunIntent()
        );
        _enterBellyState.MustPerformOnceBeforeTransitioning = true;
        _enterBellyState.FollowUpState = bellyDefend;

        attackWithAppetite.FollowUpState = _devourCravingState;
        _devourCravingState.FollowUpState = _doubleAttackState;
        _doubleAttackState.FollowUpState = _devourCravingState;
        _empowerAttackState.FollowUpState = _devourCravingState;

        bellyDefend.MustPerformOnceBeforeTransitioning = true;
        bellyDefend.FollowUpState = bellyAttack;
        bellyAttack.FollowUpState = bellyAttack;

        _moveStateMachine = new MonsterMoveStateMachine(
            [attackWithAppetite, _devourCravingState, _doubleAttackState, _empowerAttackState, _enterBellyState, bellyDefend, bellyAttack],
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

            CardModel? card = null;
            if (_lastPlayedCravingCard.TryGetValue(player, out var lastCard))
            {
                var combatState = player.PlayerCombatState;
                if (combatState != null)
                {
                    var allCards = combatState.AllCards.ToList();
                    if (allCards.Contains(lastCard) && lastCard.Affliction is CravingAffliction)
                    {
                        card = lastCard;
                    }
                }
            }

            if (card == null) continue;
            var currentPile = card.Pile?.Type ?? PileType.Hand;

            _cravingExhaustedCards.Add(card);
            CardCmd.ClearAffliction(card);
            await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), card);
            var riftCard = card.CardScope?.CreateCard<RiftCard>(player);
            if (riftCard == null) continue;
            await CardPileCmd.AddGeneratedCardToCombat(riftCard, currentPile, player);
        }

        _lastPlayedCravingCard.Clear();

        var hunger = Creature.Powers.FirstOrDefault(p => p is InsatiableHungerPower);
        if (hunger != null)
        {
            await PowerCmd.Remove(hunger);
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

        _phaseOneLoopCount++;
        _doubleAttackState.FollowUpState = _phaseOneLoopCount % 2 == 0
            ? _empowerAttackState
            : _devourCravingState;
    }

    private async Task EmpowerAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(EmpowerDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, EmpowerStrength, Creature, null);
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

    private async Task EnterBellyMove(IReadOnlyList<Creature> targets)
    {
        await EnterBelly();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_isInBelly)
        {
            await base.AfterCardPlayed(choiceContext, cardPlay);
            return;
        }

        var card = cardPlay.Card;
        if (card.Affliction is CravingAffliction)
        {
            var player = card.Owner;
            if (player != null)
            {
                _lastPlayedCravingCard[player] = card;
            }
        }

        await base.AfterCardPlayed(choiceContext, cardPlay);
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

            SetMoveImmediate(_enterBellyState, forceTransition: true);

            if (Creature.CombatState != null)
            {
                foreach (var player in Creature.CombatState.Players)
                {
                    PlayerCmd.EndTurn(player, canBackOut: false);
                }
            }
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

        if (side == CombatSide.Player && !_isInBelly)
        {
            var existingHunger = Creature.Powers.FirstOrDefault(p => p is InsatiableHungerPower);
            if (existingHunger != null)
            {
                await PowerCmd.Remove(existingHunger);
            }

            if (NextMove.Id == "DEVOUR_CRAVING")
            {
                await PowerCmd.Apply<InsatiableHungerPower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
            }
        }

        await base.AfterSideTurnStart(side, participants, combatState);
    }

    private async Task EnterBelly()
    {
        _isInBelly = true;
        _savedHp = Creature.CurrentHp;
        _savedMaxHp = Creature.MaxHp;
        _phaseOneLoopCount = 0;
        _doubleAttackState.FollowUpState = _devourCravingState;

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
        NRunMusicController.Instance?.PlayCustomMusic("event:/Neuvillette/music/AllDevouringNarwhalTheme2");
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
            .Where(p => p is not BeastOfStarsPower && p is not DevourPower && p is not StrengthPower && p is not InsatiableHungerPower && p is not HostilityPower)
            .ToList();

        foreach (var power in immediatePowers)
        {
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), power, Creature, power.Amount, power.Applier, null);
        }
        _phaseOnePowers.Clear();

        if (!_triggered25)
        {
            var threshold = _triggered75 ? 25m : 75m;
            await PowerCmd.Apply<HostilityPower>(new ThrowingPlayerChoiceContext(), Creature, threshold, Creature, null);
        }

        _delayedPowersToReapply = delayedPowers;

        Creature.HpDisplay = HpDisplay.Normal;
        Creature.SetMaxHpInternal((decimal)_savedMaxHp);
        Creature.SetCurrentHpInternal((decimal)_savedHp);

        UpdateVisuals(false);
        NRunMusicController.Instance?.PlayCustomMusic("event:/Neuvillette/music/AllDevouringNarwhalTheme");

        foreach (var player in Creature.CombatState?.Players ?? Array.Empty<Player>())
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
            }
        }
        _cravingExhaustedCards.Clear();
        _lastPlayedCravingCard.Clear();

        foreach (var kvp in _devouredCards)
        {
            var card = kvp.Key;
            if (card.DynamicVars?.TryGetValue("Damage", out var damageVar) == true)
            {
                damageVar.BaseValue = kvp.Value;
            }
        }
        _devouredCards.Clear();

        await CreatureCmd.Stun(Creature, "ATTACK_WITH_APPETITE");
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

    private void UpdateVisuals(bool isInBelly)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode?.Visuals is NarwhalCreatureVisuals visuals)
        {
            visuals.SetBellyState(isInBelly);
        }

        if (NCombatRoom.Instance?.Background is Act4Bg bg)
        {
            bg.SetBellyState(isInBelly);
        }
    }
}