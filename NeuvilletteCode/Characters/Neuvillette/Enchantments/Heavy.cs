using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Characters.Neuvillette.Enchantments;

public sealed class Heavy : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://Neuvillette/images/enchantments/neuvillette_enchantments_heavy.png"
    );
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[2]
	{
		HoverTipFactory.FromKeyword(CardKeyword.Eternal),
		HoverTipFactory.FromKeyword(CardKeyword.Retain)
	};

	public override bool CanEnchantCardType(CardType cardType)
	{
		return cardType == CardType.Attack;
	}

	protected override void OnEnchant()
	{
		base.Card.EnergyCost.SetThisCombat(1);
		base.Card.AddKeyword(CardKeyword.Eternal);
		base.Card.AddKeyword(CardKeyword.Retain);
	}

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
	{
		if (!props.IsPoweredAttack())
		{
			return 0m;
		}
		
		var baseDamage = base.Card.CanonicalInstance.DynamicVars.Damage.BaseValue;
		var upgradeBonus = originalDamage - baseDamage;
		if (upgradeBonus < 0)
		{
			upgradeBonus = 0m;
		}
		
		return 16m + upgradeBonus - originalDamage;
	}
}