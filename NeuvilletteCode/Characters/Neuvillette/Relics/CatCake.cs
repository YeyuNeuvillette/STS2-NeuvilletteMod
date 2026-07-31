using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class CatCake : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool HasUponPickupEffect => true;

    public override bool IsAllowed(IRunState runState) => NeuvilletteSettingsStore.IsSponsorRelicEnabled();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<Breed>());

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        var selectedCards = await CardSelectCmd.FromDeckForTransformation(Owner, prefs, c => CreateBreedTransformation(c, forPreview: true));
        var transformations = selectedCards.Select(c => CreateBreedTransformation(c, forPreview: false)).ToList();

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, Owner.PlayerRng.Transformations);
        }
    }

    private CardTransformation CreateBreedTransformation(CardModel original, bool forPreview)
    {
        var breed = forPreview
            ? ModelDb.Card<Breed>().ToMutable()
            : Owner.RunState.CreateCard<Breed>(Owner);
        return new CardTransformation(original, breed);
    }
}