using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class RainDewShare() : NeuvilletteCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Surge", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState != null)
        {
            foreach (var player in CombatState.Players)
            {
                if (player.Creature != null)
                {
                    var surgeValue = DynamicVars["Surge"].BaseValue;
                    var livingWaterAmount = player.Creature.GetPowerAmount<LivingWaterPower>();
                    var totalSurge = surgeValue + livingWaterAmount;

                    await CreatureCmd.Heal(player.Creature, totalSurge);
                    await PowerCmd.Apply<SurgePower>(choiceContext, player.Creature, totalSurge, player.Creature, this);

                    if (IsUpgraded)
                    {
                        await CreatureCmd.Heal(player.Creature, totalSurge);
                        await PowerCmd.Apply<SurgePower>(choiceContext, player.Creature, totalSurge, player.Creature, this);
                    }
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}