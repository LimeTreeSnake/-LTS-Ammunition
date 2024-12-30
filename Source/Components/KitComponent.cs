using System.Collections.Generic;
using Verse;
using System.Linq;
using Ammunition.Logic;
using Ammunition.Models;
using Ammunition.DefModExtensions;
using Ammunition.Settings;

namespace Ammunition.Components
{
	public class KitComponent : ThingComp
	{

		#region Fields

		private List<AmmoSlot> _bags = new List<AmmoSlot>();

		#endregion Fields

		#region Properties

		public List<AmmoSlot> Bags
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

		public override void Initialize(CompProperties properties)
		{
			base.Initialize(properties);
			InitializeBags();
		}

		private void InitializeBags()
		{
			if (_bags == null)
			{
				_bags = new List<AmmoSlot>();
			}

			if (Settings.Settings.BagSettingsDictionary.TryGetValue(this.parent.def.defName, out var bagSettings))
			{
				foreach (var bag in bagSettings.AmmoCapacities.Select(t => new AmmoSlot
				         {
					         Capacity = t,
					         Count = 0,
					         ChosenAmmo = Settings.Settings.AvailableAmmo.First(),
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

				foreach (var bag in compPropsKit.ammoCapacity.Select(t => new AmmoSlot
				         {
					         Capacity = t,
					         Count = 0,
					         ChosenAmmo = Settings.Settings.AvailableAmmo.First(),
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

			if (AmmoLogic.IsExempt(pawn.equipment.Primary.def))
			{
				return;
			}

			var ammoCategories =
				AmmoLogic.AvailableAmmoForWeapon(pawn.equipment.Primary.def);

			if (ammoCategories == null || !ammoCategories.Any())
			{
				Log.Message("There is no ammo for this weapon available but it needs ammo. Check your mod settings");
				return;
			}

			var ammoDefName = ammoCategories.RandomElement().ammoDefs.FirstOrDefault();
			var ammoDef = Settings.Settings.AvailableAmmo.FirstOrDefault(x => x.defName == ammoDefName);
			if (ammoDef == null)
			{
				return;
			}

			foreach (var t in _bags.Where(t =>
				         !AmmoLogic.WeaponDefCanUseAmmoDef(pawn.equipment.Primary.def, t.ChosenAmmo)))
			{
				t.ChosenAmmo = ammoDef;
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