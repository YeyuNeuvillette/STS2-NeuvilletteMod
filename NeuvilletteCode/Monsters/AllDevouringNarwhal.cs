using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Monsters.Afflictions;
using Neuvillette.Monsters.Cards;
using Neuvillette.Monsters.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters;

[RegisterMonster]
public class AllDevouringNarwhal : ModMonsterTemplate
{
    private bool _isInBelly;
    private bool _triggered75;
    private bool _triggered25;
    private int _savedHp;
    private int _savedMaxHp;
    private new MonsterMoveStateMachine _moveStateMachine = null!;

    private readonly Dictionary<CardModel, decimal> _devouredCards = new();

    private int AttackDamage1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 16);
    private int AttackDamage2 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 20);
    private int AttackDamage3 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 10);
    private int BellyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 160, 160);
    private int BellyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 42, 42);

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 999, 979);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://Neuvillette/scenes/all_devouring_narwhal.tscn"
    );

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
            new SingleAttackIntent(AttackDamage2)
        );

        var doubleAttack = new MoveState(
            "DOUBLE_ATTACK",
            DoubleAttackMove,
            new MultiAttackIntent(AttackDamage3, 2)
        );

        var bellyDefend = new MoveState(
            "BELLY_DEFEND",
            BellyDefendMove,
            new BuffIntent(),
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

            var allCards = player.PlayerCombatState?.AllCards.ToList() ?? new List<CardModel>();
            var cravingCards = allCards.Where(c => c.Affliction is CravingAffliction).ToList();
            if (cravingCards.Count == 0) continue;

            var card = base.RunRng.CombatCardGeneration.NextItem(cravingCards);
            if (card == null) continue;
            var currentPile = card.Pile?.Type ?? PileType.Hand;

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
        await PowerCmd.Apply<BarricadePower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
        await PowerCmd.Apply<PhantomPower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
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

        if (!_triggered75 && hpPercent <= 0.75f)
        {
            _triggered75 = true;
            await EnterBelly();
            return;
        }

        if (!_triggered25 && hpPercent <= 0.25f)
        {
            _triggered25 = true;
            await EnterBelly();
            return;
        }
    }

    private async Task EnterBelly()
    {
        _isInBelly = true;
        _savedHp = Creature.CurrentHp;
        _savedMaxHp = Creature.MaxHp;

        await CreatureCmd.SetMaxAndCurrentHp(Creature, 9999);

        SetMoveImmediate(GetBellyDefendState());
    }

    public async Task ExitBelly()
    {
        _isInBelly = false;

        Creature.SetMaxHpInternal((decimal)_savedMaxHp);
        Creature.SetCurrentHpInternal((decimal)_savedHp);

        var player = Creature.CombatState?.Players.FirstOrDefault();
        if (player != null)
        {
            var allCards = player.PlayerCombatState?.AllCards.ToList() ?? new List<CardModel>();

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

        await PowerCmd.Remove<BeastOfStarsPower>(Creature);
        await PowerCmd.Remove<DevourPower>(Creature);
    }

    public void RecordDevouredCard(CardModel card, decimal originalBaseDamage)
    {
        if (!_devouredCards.ContainsKey(card))
        {
            _devouredCards[card] = originalBaseDamage;
        }
    }

    private MoveState GetBellyDefendState()
    {
        return (MoveState)_moveStateMachine.States["BELLY_DEFEND"];
    }
}