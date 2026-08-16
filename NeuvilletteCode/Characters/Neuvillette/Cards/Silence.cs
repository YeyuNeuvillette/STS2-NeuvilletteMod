using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Neuvillette.Scripts;
namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Silence() : SubmitCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        NeuvilletteSettingsStore.IsMultiplayerCourtEnabled()
            ? CardMultiplayerConstraint.None
            : CardMultiplayerConstraint.SingleplayerOnly;
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Submit];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/Neuvillette/sfx/Silence");

        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", "event:/Neuvillette/sfx/WaterSplashHit")
            .BeforeDamage(() =>
            {
                var vfx = NeuvilletteAttackVfx.Create(cardPlay.Target);
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
                return Task.CompletedTask;
            })
            .Execute(choiceContext);

        var drawPileCards = PileType.Draw.GetPile(Owner).Cards.OrderBy(c => c.Rarity).ThenBy(c => c.Id).ToList();
        var selectedCard = (await CardSelectCmd.FromSimpleGrid(choiceContext, drawPileCards, Owner, GetSubmitSelectionPrefs()))
            .FirstOrDefault();

        if (selectedCard != null)
            await PerformSubmit(choiceContext, selectedCard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
