using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System;
using Ammunition.Defs;
using Ammunition.Components;
using Ammunition.DefModExtensions;
using Ammunition.Models;
using Ammunition.Settings;
using Ammunition.Things;

namespace Ammunition.Logic
{
	[StaticConstructorOnStartup]
	public static class AmmoLogic
	{
		public static bool HaveAmmo = true;

		private static bool TechLevelEqualCategory(TechLevel tech, AmmoTypes category)
		{
			switch (tech)
			{
				case TechLevel.Undefined:
					return category == AmmoTypes.None;


				case TechLevel.Animal:
					return category == AmmoTypes.None;


				case TechLevel.Neolithic:
					return category == AmmoTypes.Primitive;


				case TechLevel.Medieval:
					return category == AmmoTypes.Medieval;


				case TechLevel.Industrial:
					return category == AmmoTypes.Industrial;


				case TechLevel.Spacer:
					return category == AmmoTypes.Spacer;


				case TechLevel.Ultra:
					return category == AmmoTypes.Ultratech;


				case TechLevel.Archotech:
					return category == AmmoTypes.Archotech;


				default:
					return category == AmmoTypes.None;
			}
		}

		public static bool CanBeLootedByColony(Thing thing)
		{
			if (!(thing is IStrippable strippable))
			{
				return false;
			}

			if (!strippable.AnythingToStrip())
			{
				return false;
			}

			if (thing is Kit kit)
			{
				if (kit.KitComp?.Bags?.Any(item => item.Count > 0) == true)
				{
					return true;
				}
			}

			var pawn = thing as Pawn ?? (thing as Corpse)?.InnerPawn;
			if (pawn == null)
			{
				return false;
			}

			if (pawn.IsQuestLodger())
			{
				return false;
			}

			if (pawn.apparel?.AnyApparel == true &&
			    !GetWornKits(pawn).Any(x => x.KitComp?.Bags?.Any(y => y.Count > 0) == true))
			{
				return false;
			}

			if (pawn.Downed || pawn.Dead)
			{
				return true;
			}

			return pawn.IsPrisonerOfColony && pawn.guest?.PrisonerIsSecure == true;
		}

		public static void LootAmmo(Thing thing)
		{
			var pawn = thing as Pawn ?? (thing as Corpse)?.InnerPawn;
			if (pawn == null)
			{
				return;
			}

			var kits = GetWornKits(pawn)?.ToList();
			if (kits == null || kits.Count <= 0)
			{
				return;
			}

			foreach (var kit in kits)
			{
				EmptyKitAt(kit.KitComp, kit.SpawnedParentOrMe.PositionHeld);
			}
		}

		public static void EmptyKitAt(KitComponent kit, IntVec3 pos)
		{
			foreach (var t in kit.Bags)
			{
				EmptyBagAt(t, pos);
			}
		}

		public static void EmptyBagAt(AmmoSlot ammoSlot, IntVec3 pos)
		{
			if (ammoSlot.Count < 1)
			{
				return;
			}

			var ammo = ThingMaker.MakeThing(ammoSlot.ChosenAmmo);
			ammo.stackCount = ammoSlot.Count;
			GenPlace.TryPlaceThing(ammo, pos, Find.CurrentMap,
				ThingPlaceMode.Near);

			ammoSlot.Count = 0;
		}

		private static int GetRandomWeightedIndex(List<int> weights)
		{
			if (weights.NullOrEmpty())
			{
				return 0;
			}

			var sum = weights.Sum();

			if (sum < 1)
			{
				return 0;
			}

			float r = Rand.Range(0, sum);

			foreach (var t in weights)
			{
				if (t <= r)
				{
					return t;
				}

				r -= t;
			}

			return 0;
		}

		public static int GetAmmoWeight(ThingDef ammo)
		{
			IEnumerable<AmmoCategoryDef> categoriesWithAmmo =
				Settings.Settings.AmmoToCategoriesDictionary.TryGetValue(ammo);
			return categoriesWithAmmo.EnumerableNullOrEmpty() ? 1 : categoriesWithAmmo.Max(x => x.ammoWeight);
		}

		private static void LoadSpawnedKit(Kit apparel, AmmoCategoryDef def, Pawn pawn = null)
		{
			foreach (var bag in apparel.KitComp.Bags)
			{
				var ammoDef = def.weightList.NullOrEmpty()
					? def.ammoDefs.RandomElement()
					: def.ammoDefs[GetRandomWeightedIndex(def.weightList)];

				bag.ChosenAmmo = Settings.Settings.AvailableAmmo.FirstOrDefault(x => x.defName == ammoDef);
				if (bag.ChosenAmmo != null)
				{
					bag.MaxCount = bag.Capacity;
					bag.Count = (int)(Rand.Range(bag.Capacity * (Settings.Settings.Range.min / 100f),
						                  bag.Capacity) *
					                  (Settings.Settings.Range.max / 100f));

					if (pawn != null)
					{
						apparel.SetStyleDef(pawn.Ideo?.GetStyleFor(apparel.def));
					}
				}
				else
				{
					Log.Message("There are somehow no ammo within randomly chosen category.");
					apparel.Destroy();
				}
			}
		}

		public static void EquipPawn(Pawn p)
		{
			try
			{
				if (p?.RaceProps == null || p.RaceProps.IsMechanoid || p.RaceProps.Animal)
				{
					return;
				}

				if (p.apparel == null || p.equipment == null)
				{
					return;
				}

				var weaponDef = p.equipment.Primary?.def;
				var verb = weaponDef?.Verbs?.FirstOrDefault(
					x => x.verbClass.IsSubclassOf(typeof(Verb_LaunchProjectile)));

				if (weaponDef == null || verb == null)
				{
					return;
				}

				Kit apparel = null;
				//Try find if pawn already have a kit equipped.
				if (p.apparel.AnyApparel)
				{
					apparel = (Kit)p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
				}

				var categories = Settings.Settings.AmmoCategoryDefs.Where(x =>
						Settings.Settings.CategoryWeaponDictionary.TryGetValue
							(x.defName, out var wep) &&
						!wep.NullOrEmpty() &&
						wep.TryGetValue(weaponDef.defName, out var res) &&
						res)
					.ToList();

				if (categories.NullOrEmpty())
				{
					return;
				}

				// We have a kit on said pawn already
				if (apparel != null)
				{
					LoadSpawnedKit(apparel, categories.RandomElement(), p);
					return;
				}

				// Find a suitable kit to equip
				ThingDef kit = null;
				var wearableKits = Settings.Settings.AvailableKits
					.Where(y => y.GetCompProperties<CompProps_Kit>().canBeGenerated &&
					            p.apparel.CanWearWithoutDroppingAnything(y))
					.ToList();

				if (wearableKits.NullOrEmpty())
				{
					Log.Message("LTS_Ammo - No kits available for pawn");
					return;
				}

				if (!p.ageTracker.Adult)
				{
					kit = wearableKits.Where(t => t.apparel.developmentalStageFilter.HasFlag
							(DevelopmentalStage.Child))
						.RandomElement();
				}
				else if (Settings.Settings.UseAmmoPerBullet && verb.burstShotCount > 1)
				{
					kit = wearableKits.FirstOrDefault(
						y => y.GetCompProperties<CompProps_Kit>()?.ammoCapacity?.Sum() / verb.burstShotCount > 20);
				}

				if (kit == null)
				{
					kit = wearableKits.RandomElement();
				}

				var stuff = GenStuff.AllowedStuffsFor(kit)?.RandomElement();
				apparel = (Kit)ThingMaker.MakeThing(kit,
					stuff ?? ThingDefOf.Cloth);

				if (apparel == null)
				{
					return;
				}

				p.apparel.Wear(apparel);
				LoadSpawnedKit(apparel, categories.RandomElement(), p);
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("LTS_EquipPawn error, contact LTS with this! - " + ex.Message);
				}
			}
		}

		internal static void Initialize(bool load = true)
		{
			try
			{
				var changes = false;
				if (load)
				{
					AmmoSettingsIO.Load();
				}

				if (!Settings.Settings.AmmoCategoryDefs.Any() || !Settings.Settings.AvailableAmmo.Any())
				{
					HaveAmmo = false;
					Log.Error("There are no ammo mods installed, only the framework!");
					return;
				}

				foreach (var category in Settings.Settings.AmmoCategoryDefs)
				{
					// Create a dictionary for fast tracking of what ammo is in what category.
					foreach (var ammoDef in category.ammoDefs
						         .Select(ammoStringDef =>
							         Settings.Settings.AvailableAmmo.FirstOrDefault(x => x.defName == ammoStringDef))
						         .Where(ammoDef => ammoDef != null))
					{
						if (!Settings.Settings.AmmoToCategoriesDictionary.TryGetValue(ammoDef, out var categoryList))
						{
							categoryList = new List<AmmoCategoryDef>();
							Settings.Settings.AmmoToCategoriesDictionary[ammoDef] = categoryList;
						}

						if (!categoryList.Contains(category))
						{
							categoryList.Add(category);
						}
					}

					// Check if the CategoryWeaponDictionary contains the category
					if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName, out var weaponDict))
					{
						weaponDict = new Dictionary<string, bool>();
						Settings.Settings.CategoryWeaponDictionary[category.defName] = weaponDict;
						changes = true;
					}

					// Iterate through the available weapons
					foreach (var weapon in Settings.Settings.AvailableProjectileWeapons)
					{
						if (weaponDict.ContainsKey(weapon.defName))
						{
							continue;
						}

						var isAssigned = category.includeWeaponDefs.Contains(weapon.defName) ||
						                 (category.autoAssignable &&
						                  TechLevelEqualCategory(weapon.techLevel, category.ammoType));

						weaponDict[weapon.defName] = isAssigned && !category.excludeWeaponDefs.Contains(weapon.defName);
						changes = true;
					}
				}

				foreach (var weapon in Settings.Settings.AvailableProjectileWeapons)
				{
					var hasAmmo = WeaponHasAmmo(weapon);

					if (Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.defName, out var isExempt))
					{
						if (hasAmmo || isExempt)
						{
							continue;
						}

						Settings.Settings.ExemptionWeaponDictionary[weapon.defName] = true;
						Log.Message(
							$"{weapon.label} - has no usable ammo for Ammunition Framework, thus it has been exempt.");
						changes = true;
					}
					else
					{
						var shouldBeExempt = weapon.HasModExtension<ExemptAmmoUsageExtension>() || !hasAmmo;
						Settings.Settings.ExemptionWeaponDictionary[weapon.defName] = shouldBeExempt;
						changes = true;
					}
				}

				foreach (var def in Settings.Settings.AvailableKits)
				{
					if (Settings.Settings.BagSettingsDictionary.ContainsKey(def.defName))
					{
						continue;
					}

					var bagSettings = new BagSettings();
					if (def.comps.FirstOrDefault(x => x is CompProps_Kit) is CompProps_Kit kitProp)
					{
						bagSettings.AmmoCapacities.AddRange(kitProp.ammoCapacity);
					}

					Settings.Settings.BagSettingsDictionary.Add(def.defName, bagSettings);
					changes = true;
				}

				ResetHyperlinks();
				if (changes)
				{
					AmmoSettingsIO.Save();
				}
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error initializing Ammunition Framework: " + ex.Message + "\n" + ex.StackTrace);
				}
			}
		}

		private static bool WeaponHasAmmo(ThingDef weapon)
		{
			foreach (var category in Settings.Settings.AmmoCategoryDefs)
			{
				if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName, out var dic))
				{
					continue;
				}

				if (dic != null && dic.TryGetValue(weapon.defName, out var value) && value)
				{
					return true;
				}
			}

			return false;
		}

		internal static void ResetInitialize()
		{
			Settings.Settings.CategoryWeaponDictionary.Clear();
			Settings.Settings.ExemptionWeaponDictionary.Clear();
			Settings.Settings.BagSettingsDictionary.Clear();
			Settings.Settings.AmmoToCategoriesDictionary.Clear();
			Initialize(false);
		}

		private static void ResetHyperlinks()
		{
			if (!Settings.Settings.AvailableProjectileWeapons.Any() || !Settings.Settings.AvailableAmmo.Any())
			{
				return;
			}

			foreach (var def in Settings.Settings.AvailableProjectileWeapons)
			{
				ResetHyperLinksForWeapon(def);
			}
		}

		private static void ResetHyperLinksForWeapon(ThingDef weaponDef)
		{
			// Remove invalid ammo links
			weaponDef.descriptionHyperlinks?.RemoveAll(link =>
				link.def is ThingDef ammoDef && Settings.Settings.AvailableAmmo.Contains(ammoDef)
				                             && !WeaponDefCanUseAmmoDef(weaponDef, ammoDef));

			// Stop if weapon is exempt from using ammo
			if (IsExempt(weaponDef))
			{
				return;
			}

			// Add valid ammo links
			foreach (var ammoDef in Settings.Settings.AvailableAmmo.Where(ammoDef =>
				         WeaponDefCanUseAmmoDef(weaponDef, ammoDef)))
			{
				if (weaponDef.descriptionHyperlinks == null)
				{
					weaponDef.descriptionHyperlinks = new List<DefHyperlink>();
				}

				weaponDef.descriptionHyperlinks.Add(ammoDef);

				// Add the associated projectile (if any)
				var projectile = ammoDef.GetModExtension<AmmunitionExtension>()?.bulletDef
				                 ?? weaponDef.Verbs.FirstOrDefault()?.defaultProjectile;
				if (projectile != null)
				{
					weaponDef.descriptionHyperlinks.Add(projectile);
				}
			}
		}

		/// <summary>
		/// Determines whether a pawn has the required ammo for the specified weapon. Optionally, this method can consume ammo.
		/// </summary>
		/// <param name="pawn">The pawn using the weapon.</param>
		/// <param name="weapon">The weapon that requires ammo.</param>
		/// <param name="kitComp">The kit component being used (if any). Set to null if no viable kit is found.</param>
		/// <param name="consumeAmmo">If true, the method will decrease the ammo count when successful.</param>
		/// <returns>
		/// True if the pawn does not need ammo (e.g., exempt or has enough ammo available); 
		/// otherwise, false if no viable ammo is found.
		/// </returns>
		public static bool AmmoCheck(Pawn pawn, Thing weapon, out KitComponent kitComp, bool consumeAmmo)
		{
			kitComp = null;

			try
			{
				// Early returns for destroyed pawns or weapons
				if (pawn.DestroyedOrNull() || weapon.DestroyedOrNull())
				{
					return true;
				}

				// Mechanoids and animals are exempt from ammo (custom logic could be added here)
				if (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
				{
					return true;
				}

				// If the weapon is exempt from ammo usage
				if (IsExempt(weapon.def))
				{
					return true;
				}
				
				// If pawn has no apparel, they can't wear kits, hence exempt from ammo
				if (pawn.apparel == null || !pawn.apparel.AnyApparel)
				{
					return false;
				}
				
				// Get worn kits and check for ammo
				var kits = GetWornKits(pawn);
				if (!kits.Any())
				{
					return false;
				}

				foreach (var kit in kits)
				{
					kitComp = kit.KitComp;

					// Find the first viable ammo slot in the kit
					var ammoSlot = kitComp.Bags.FirstOrDefault(
						t => t.Use && t.ChosenAmmo != null && t.Count > 0 &&
						     WeaponDefCanUseAmmoDef(weapon.def, t.ChosenAmmo));

					// If viable ammo is found
					if (ammoSlot == null)
					{
						continue;
					}

					// If we're consuming ammo, decrease the count based on whether the pawn is a colonist or NPC
					if (consumeAmmo)
					{
						if (pawn.IsColonist || Settings.Settings.NpcUseAmmo)
						{
							ammoSlot.Count--;
						}
					}

					// Update the last used ammo
					kitComp.LastUsedAmmo = ammoSlot.ChosenAmmo;
					return true;
				}

				// No viable ammo found in kits
				kitComp = null;
				return false;
			}
			catch (Exception ex)
			{
				// Debug logging for failure
				if (Settings.Settings.DebugLogs)
				{
					Log.Error($"Failure in AmmoCheck for {weapon?.def?.defName} and {pawn?.Name}: {ex.Message}");
					if (kitComp != null)
					{
						Log.Error($"{kitComp.parent.def.defName} has components but no viable ammo.");
						kitComp = null;
					}
				}

				// Default to no usable ammo if exception occurs
				kitComp = null;
				return false;
			}
		}


		/// <summary>
		/// If a weapon is monitored by this mod and if so, whether it is exempt or not.
		/// </summary>
		/// <param name="weapon">The weapon def in question</param>
		/// <returns>true is this weapon can fire due to being exempt</returns>
		public static bool IsExempt(ThingDef weapon)
		{
			return !Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.defName, out var exempt) || exempt;
		}

		public static bool IsViableWeaponDef(ThingDef weapon)
		{
			return !weapon.IsMeleeWeapon &&
			       !weapon.IsBuildingArtificial &&
			       !weapon.Verbs.Any(y => y.verbClass == typeof(Verb_ShootOneUse)) &&
			       (weapon.HasComp(typeof(CompEquippable)) ||
			        weapon.HasComp(typeof(CompEquippableAbility)) ||
			        weapon.HasComp(typeof(CompEquippableAbilityReloadable))) &&
			       weapon.IsWithinCategory(ThingCategoryDefOf.Weapons);
		}

		public static void FixCategoryWeaponDictionaries()
		{
			foreach (var weaponDef in Settings.Settings.AvailableProjectileWeapons)
			{
				var hasAmmo = false;

				// Check if the weapon needs ammo in any category
				foreach (var categoryDictionary in Settings.Settings.CategoryWeaponDictionary)
				{
					if (categoryDictionary.Value.TryGetValue(weaponDef.defName, out var needAmmo) && needAmmo)
					{
						hasAmmo = true;
						break; // No need to check further categories if ammo is found
					}
				}

				// Update the exemption dictionary if no ammo is needed
				Settings.Settings.ExemptionWeaponDictionary.SetOrAdd(weaponDef.defName, !hasAmmo);

				// Reset hyperlinks for the weapon
				ResetHyperLinksForWeapon(weaponDef);
			}
		}

		public static List<AmmoCategoryDef> AvailableAmmoForWeapon(ThingDef weapon)
		{
			var list = new List<AmmoCategoryDef>();
			var ammoCatList = Settings.Settings.AmmoCategoryDefs.ToList();
			for (var i = 0; i < ammoCatList.Count(); i++)
			{
				if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(ammoCatList[i].defName,
					    out var dic))
				{
					continue;
				}

				if (!dic.TryGetValue(weapon.defName, out var res))
				{
					continue;
				}

				if (res)
				{
					list.Add(ammoCatList[i]);
				}
			}

			return list;
		}

		public static bool WeaponDefCanUseAmmoDef(ThingDef weapon, ThingDef ammo)
		{
			if (!Settings.Settings.AmmoToCategoriesDictionary.TryGetValue(ammo,
				    out var availableCategories) ||
			    !availableCategories.Any())
			{
				return false;
			}

			for (var c = 0; c < availableCategories.Count(); c++)
			{
				if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(availableCategories[c].defName,
					    out var dic))
				{
					continue;
				}

				if (dic.TryGetValue(weapon.defName, out var res) && res)
				{
					return true;
				}
			}

			return false;
		}

		public static List<Kit> GetWornKits(Pawn pawn)
		{
			return pawn.apparel.WornApparel.Where(x => x.TryGetComp<KitComponent>() != null)
				.Select(app => app as Kit)
				.ToList();
		}
	}
}