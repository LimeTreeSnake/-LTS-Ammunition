using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using Ammunition.Logic;
using Ammunition.Models;
using Ammunition.DefModExtensions;
using Ammunition.Defs;
using UnityEngine;

namespace Ammunition.Components
{
	public class KitComponent : ThingComp
	{

		#region Fields

		private List<Bag> _bags = new List<Bag>();

		#endregion Fields

		#region Properties

		public List<Bag> Bags
		{
			get
			{
				return _bags;
			}
		}

		public CompProps_Kit Props => (CompProps_Kit)this.props;

		public ThingDef LastUsedAmmo { get; set; }

		public ThingDef LastUsedBullet => LastUsedAmmo?.GetModExtension<AmmunitionExtension>()?.bulletDef;

		#endregion Properties

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			InitializeBags();
		}

		private void InitializeBags()
		{
			if (_bags == null)
			{
				_bags = new List<Bag>();
			}

			if (Settings.Settings.BagSettingsDictionary.TryGetValue(this.parent.def.defName, out List<int> bagSettings))
			{
				foreach (Bag bag in bagSettings.Select(t => new Bag
				         {
					         ChosenAmmo =
						         Settings.Settings.InitialAmmoType == null
							         ? Settings.Settings.GetDefaultAmmo()
							         : Settings.Settings.InitialAmmoType,
					         Count = 0,
					         Capacity = t,
					         MaxCount = t,
					         Use = true
				         }))
				{
					_bags.Add(bag);
				}
			}
			else
			{
				if (!(this.props is CompProps_Kit compPropsKit))
				{
					return;
				}

				foreach (Bag bag in compPropsKit.ammoCapacity.Select(t => new Bag
				         {
					         ChosenAmmo =
						         Settings.Settings.InitialAmmoType == null
							         ? Settings.Settings.GetDefaultAmmo()
							         : Settings.Settings.InitialAmmoType,
					         Count = 0,
					         Capacity = t,
					         MaxCount = t,
					         Use = true
				         }))
				{
					_bags.Add(bag);
				}
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref _bags, "Bags", LookMode.Deep);
		}

		public override void Notify_Unequipped(Pawn pawn)
		{
			base.Notify_Unequipped(pawn);
			if (pawn.Spawned)
			{
				AmmoLogic.EmptyKitAt(this, pawn.PositionHeld);
			}

		}

		public override void Notify_Equipped(Pawn pawn)
		{
			base.Notify_Equipped(pawn);
			if (!pawn.Spawned || pawn.equipment?.Primary == null)
			{
				return;
			}

			if (AmmoLogic.IsExempt(pawn.equipment.Primary.def.defName))
			{
				return;
			}

			if (Settings.Settings.InitialAmmoType != null &&
			    AmmoLogic.WeaponDefCanUseAmmoDef(pawn.equipment.Primary.def.defName,
				    Settings.Settings.InitialAmmoType.defName))
			{
				foreach (Bag t in _bags)
				{
					t.ChosenAmmo = Settings.Settings.InitialAmmoType;
				}
			}
			else
			{
				List<AmmoCategoryDef> ammoCategories =
					AmmoLogic.AvailableAmmoForWeapon(pawn.equipment.Primary.def.defName);

				if (ammoCategories == null || !ammoCategories.Any())
				{
					return;
				}

				string ammoDefName = ammoCategories.RandomElement().ammoDefs.RandomElement();
				ThingDef ammoDef = AmmoLogic.AvailableAmmo.FirstOrDefault(x => x.defName == ammoDefName);
				if (ammoDef == null)
				{
					return;
				}

				foreach (Bag t in _bags)
				{
					t.ChosenAmmo = ammoDef;
				}

			}
		}
	}

	public class CompProps_Kit : CompProperties
	{
		public List<int> ammoCapacity;
		public bool canBeGenerated = true;

		public CompProps_Kit()
		{
			compClass = typeof(KitComponent);
		}
	}
}