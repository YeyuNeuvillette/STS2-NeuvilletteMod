using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using Neuvillette.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Characters.Neuvillette.Relics;

public class MeditateRestSiteOption : ModRestSiteOptionTemplate
{
    public override string OptionId => "NEUVILLETTE_MEDITATE";

    public override string? CustomIconPath =>
        "ui/rest_site/option_neuvillette_meditate.png".ImagePath();

    public override LocString? CustomTitle => new("relics", "NEUVILLETTE_REST_SITE_OPTION_MEDITATE.name");

    public override LocString Description
    {
        get
        {
            var key = IsEnabled
                ? "NEUVILLETTE_REST_SITE_OPTION_MEDITATE.description"
                : "NEUVILLETTE_REST_SITE_OPTION_MEDITATE.descriptionDisabled";
            return new LocString("relics", key);
        }
    }

    public override bool IsEnabled =>
        Owner.GetRelic<Persona>() != null && Owner.GetRelic<Memory>() == null;

    public MeditateRestSiteOption(Player owner) : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        var selected = (await CardSelectCmd.FromDeckForTransformation(
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1)))
            .FirstOrDefault();
        if (selected == null) return false;

        var replacement = CardFactory.CreateRandomCardForTransform(
            selected, isInCombat: false, Owner.RunState.Rng.Niche);
        await CardCmd.Transform(selected, replacement);
        await RelicCmd.Obtain<Memory>(Owner);

        return true;
    }
}