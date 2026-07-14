using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using Neuvillette.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Characters.Neuvillette.Relics;

public class ForgeSwordRestSiteOption : ModRestSiteOptionTemplate
{
    public override string OptionId => "NEUVILLETTE_FORGE_SWORD";

    public override string? CustomIconPath =>
        "ui/rest_site/option_neuvillette_forge_sword.png".ImagePath();

    public override LocString? CustomTitle => new("relics", "NEUVILLETTE_REST_SITE_OPTION_FORGE_SWORD.name");

    public override LocString Description
    {
        get
        {
            var key = IsEnabled
                ? "NEUVILLETTE_REST_SITE_OPTION_FORGE_SWORD.description"
                : "NEUVILLETTE_REST_SITE_OPTION_FORGE_SWORD.descriptionDisabled";
            return new LocString("relics", key);
        }
    }

    public override bool IsEnabled =>
        Owner.GetRelic<Persona>() != null &&
        Owner.GetRelic<Soul>() != null &&
        Owner.GetRelic<Memory>() != null &&
        Owner.GetRelic<Wish>() != null;

    public ForgeSwordRestSiteOption(Player owner) : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        var persona = Owner.GetRelic<Persona>();
        var soul = Owner.GetRelic<Soul>();
        var memory = Owner.GetRelic<Memory>();
        var wish = Owner.GetRelic<Wish>();

        if (persona != null) await RelicCmd.Remove(persona);
        if (soul != null) await RelicCmd.Remove(soul);
        if (memory != null) await RelicCmd.Remove(memory);
        if (wish != null) await RelicCmd.Remove(wish);

        await RelicCmd.Obtain<NarzissenkreuzSword>(Owner);
        return true;
    }
}