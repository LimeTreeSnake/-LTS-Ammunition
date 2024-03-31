﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System;
using Ammunition.Defs;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Ammunition.Components;
using Ammunition.DefModExtensions;
using Ammunition.Models;
using Ammunition.Things;

namespace Ammunition.Logic
{
	[StaticConstructorOnStartup]
	public static class AmmoLogic
	{

		/// <summary>
		/// Returns all equipable weapons that fires projectiles.
		/// </summary>
		public static List<ThingDef> AvailableProjectileWeapons = new List<ThingDef>();

		/// <summary>
		/// Returns all thingdefs with an ammunition component.
		/// </summary>
		public static List<ThingDef> AvailableAmmo = new List<ThingDef>();

		/// <summary>
		/// Returns all thingdefs with a kit component.
		/// </summary>
		public static List<ThingDef> AvailableKits = new List<ThingDef>();

		/// <summary>
		/// Returns all AmmoCategoryDefs.
		/// </summary>
		public static List<AmmoCategoryDef> AmmoCategoryDefs = new List<AmmoCategoryDef>();

		/// <summary>
		/// Generated by Initialize at boot-time.
		/// </summary>
		private static readonly Dictionary<string, List<AmmoCategoryDef>> _ammoToCategoriesDictionary =
			new Dictionary<string, List<AmmoCategoryDef>>();

		private static readonly List<ThingDef> _noneAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _primitiveAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _medievalAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _industrialAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _explosiveAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _spacerAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _archotechAmmo = new List<ThingDef>();
		private static readonly List<ThingDef> _ultratechAmmo = new List<ThingDef>();

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

		internal static List<ThingDef> AmmoTypesList(AmmoTypes type)
		{
			switch (type)
			{
				case AmmoTypes.None:
					return _noneAmmo;


				case AmmoTypes.Primitive:
					return _primitiveAmmo;


				case AmmoTypes.Medieval:
					return _medievalAmmo;


				case AmmoTypes.Industrial:
					return _industrialAmmo;


				case AmmoTypes.Explosive:
					return _explosiveAmmo;


				case AmmoTypes.Spacer:
					return _spacerAmmo;


				case AmmoTypes.Ultratech:
					return _ultratechAmmo;


				case AmmoTypes.Archotech:
					return _archotechAmmo;
			}

			return null;
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
				if (Enumerable.Any(kit.KitComp.Bags, item => item.Count > 0))
				{
					return true;
				}
			}

			Pawn pawn = thing as Pawn ?? (thing as Corpse)?.InnerPawn;
			if (pawn == null)
			{
				return false;
			}

			if (pawn.IsQuestLodger())
			{
				return false;
			}

			if (pawn.apparel.AnyApparel &&
			    !GetWornKits(pawn).Any(x => x.KitComp.Bags.Any(y => y.Count > 0)))
			{
				return false;
			}

			if (pawn.Downed || pawn.Dead)
			{
				return true;
			}

			return pawn.IsPrisonerOfColony && pawn.guest.PrisonerIsSecure;
		}

		public static void LootAmmo(Thing thing)
		{
			Pawn pawn = thing as Pawn ?? (thing as Corpse)?.InnerPawn;
			if (pawn == null)
			{
				return;
			}

			var kits = GetWornKits(pawn)?.ToList();
			if (kits == null || kits.Count <= 0)
			{
				return;
			}

			foreach (Kit kit in kits)
			{
				EmptyKitAt(kit.KitComp, kit.SpawnedParentOrMe.PositionHeld);
			}
		}

		public static void EmptyKitAt(KitComponent kit, IntVec3 pos)
		{
			foreach (AmmoSlot t in kit.Bags)
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

			Thing ammo = ThingMaker.MakeThing(ammoSlot.ChosenAmmo);
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

			int sum = weights.Sum();

			if (sum < 1)
			{
				return 0;
			}

			float r = Rand.Range(0, sum);

			foreach (int t in weights)
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
			IEnumerable<AmmoCategoryDef> categoriesWithAmmo = _ammoToCategoriesDictionary.TryGetValue(ammo.defName);
			return categoriesWithAmmo.EnumerableNullOrEmpty() ? 1 : categoriesWithAmmo.Max(x => x.ammoWeight);
		}

		private static void LoadSpawnedKit(Kit apparel, AmmoCategoryDef def, Pawn pawn = null)
		{
			foreach (AmmoSlot bag in apparel.KitComp.Bags)
			{
				string ammoDef = def.weightList.NullOrEmpty()
					? def.ammoDefs.RandomElement()
					: def.ammoDefs[GetRandomWeightedIndex(def.weightList)];

				bag.ChosenAmmo = AvailableAmmo.FirstOrDefault(x => x.defName == ammoDef);
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
				if (p.RaceProps == null || p.RaceProps.IsMechanoid || p.RaceProps.Animal)
				{
					return;
				}

				if (p.apparel == null || p.equipment == null)
				{
					return;
				}

				ThingDef weaponDef = p.equipment.Primary?.def;
				VerbProperties verb = weaponDef?.Verbs?.FirstOrDefault(
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

				var categories = AmmoCategoryDefs.Where(x =>
						Settings.Settings.CategoryWeaponDictionary.TryGetValue
							(x.defName, out Dictionary<string, bool> wep) &&
						!wep.NullOrEmpty() &&
						wep.TryGetValue(weaponDef.defName, out bool res) &&
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
				var wearableKits = AvailableKits
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

				ThingDef stuff = GenStuff.AllowedStuffsFor(kit)?.RandomElement();
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
				bool changes = false;
				if (load)
				{
					Load();
				}

				CheckInternalDictionaryIntegrity();
				GenerateLists();
				if (!AmmoCategoryDefs.Any() || !AvailableAmmo.Any())
				{
					HaveAmmo = false;
					Log.Error("There are no ammo mods installed, only the framework!");
					return;
				}

				foreach (AmmoCategoryDef category in AmmoCategoryDefs)
				{
					//Create a dictionary for fast tracking of what ammo is in what category.
					foreach (string ammoStringDef in from ammoStringDef in category.ammoDefs
					         let ammoDef = AvailableAmmo.FirstOrDefault(x => x.defName == ammoStringDef)
					         where ammoDef != null
					         select ammoStringDef)
					{
						if (!_ammoToCategoriesDictionary.ContainsKey(ammoStringDef))
						{
							_ammoToCategoriesDictionary.Add(ammoStringDef, new List<AmmoCategoryDef>());
						}

						_ammoToCategoriesDictionary[ammoStringDef].Add(category);
					}

					if (!Settings.Settings.CategoryWeaponDictionary.ContainsKey(category.defName))
					{
						Settings.Settings.CategoryWeaponDictionary.Add(category.defName,
							new Dictionary<string, bool>());

						changes = true;
					}

					Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName,
						out Dictionary<string, bool> dic);

					if (dic == null)
					{
						Settings.Settings.CategoryWeaponDictionary.SetOrAdd(category.defName,
							new Dictionary<string, bool>());

						changes = true;
					}

					foreach (ThingDef weapon in AvailableProjectileWeapons)
					{
						if (dic == null || dic.ContainsKey(weapon.defName))
						{
							continue;
						}

						bool assigned = (category.includeWeaponDefs.Contains(weapon.defName) ||
						                 (category.autoAssignable &&
						                  TechLevelEqualCategory(weapon.techLevel, category.ammoType)));

						dic.Add(weapon.defName, assigned && !category.excludeWeaponDefs.Contains(weapon.defName));
						changes = true;
					}
				}

				foreach (ThingDef weapon in AvailableProjectileWeapons)
				{
					bool hasAmmo = false;
					foreach (AmmoCategoryDef category in AmmoCategoryDefs)
					{
						Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName,
							out Dictionary<string, bool> dic);

						if (dic == null)
						{
							continue;
						}

						dic.TryGetValue(weapon.defName, out bool value);
						if (value)
						{
							hasAmmo = true;
						}
					}

					if (Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.defName, out bool isExempt))
					{
						if (hasAmmo || isExempt)
						{
							continue;
						}

						Settings.Settings.ExemptionWeaponDictionary.SetOrAdd(weapon.defName, true);
						Log.Message(weapon.label + " - have no usable ammo for Ammunition Framework thus been exempt.");
						changes = true;
					}
					else
					{
						Settings.Settings.ExemptionWeaponDictionary.Add(weapon.defName,
							weapon.HasModExtension<ExemptAmmoUsageExtension>() || !hasAmmo);

						changes = true;
					}
				}

				foreach (ThingDef def in AvailableKits)
				{
					if (Settings.Settings.BagSettingsDictionary.ContainsKey(def.defName))
					{
						continue;
					}

					var intList = new List<int>();
					if (def.comps.FirstOrDefault(x => x is CompProps_Kit) is CompProps_Kit kitProp)
					{
						intList.AddRange(kitProp.ammoCapacity);
					}

					Settings.Settings.BagSettingsDictionary.Add(def.defName, intList);
					changes = true;
				}

				ResetHyperlinks();
				if (changes)
				{
					Save();
				}
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error initializing Ammunition Framework: " + ex.Message);
				}
			}
		}

		private static void GenerateLists()
		{
			AmmoCategoryDefs = DefDatabase<AmmoCategoryDef>.AllDefsListForReading;
			AvailableProjectileWeapons = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(IsViableWeaponDef)
				.ToList();

			AvailableAmmo = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(x => x.HasModExtension<AmmunitionExtension>())
				.ToList();

			AvailableKits = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(x => x.HasComp(typeof(KitComponent)))
				.ToList();
		}

		private static void CheckInternalDictionaryIntegrity()
		{
			if (Settings.Settings.ExemptionWeaponDictionary == null)
			{
				Settings.Settings.ExemptionWeaponDictionary = new Dictionary<string, bool>();
			}

			if (Settings.Settings.CategoryWeaponDictionary == null)
			{
				Settings.Settings.CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
			}

			if (Settings.Settings.BagSettingsDictionary == null)
			{
				Settings.Settings.BagSettingsDictionary = new Dictionary<string, List<int>>();
			}
		}

		internal static void ResetInitialize()
		{
			Settings.Settings.CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
			Settings.Settings.ExemptionWeaponDictionary = new Dictionary<string, bool>();
			Settings.Settings.BagSettingsDictionary = new Dictionary<string, List<int>>();
			Initialize(false);
		}

		private static void ResetHyperlinks()
		{
			if (!AvailableProjectileWeapons.Any() || !AvailableAmmo.Any())
			{
				return;
			}

			foreach (ThingDef def in AvailableProjectileWeapons)
			{
				ResetHyperLinksForWeapon(def);
			}
		}

		internal static void ResetHyperLinksForWeapon(ThingDef def)
		{
			if (def.descriptionHyperlinks != null && def.descriptionHyperlinks.Any())
			{
				for (int i = 0; i < def.descriptionHyperlinks.Count; i++)
				{
					if (!AvailableAmmo.Contains(def.descriptionHyperlinks[i].def))
					{
						continue;
					}

					def.descriptionHyperlinks.RemoveAt(i);
					i--;
				}
			}

			if (IsExempt(def.defName))
			{
				return;
			}

			foreach (ThingDef ammoDef in AvailableAmmo.Where(ammoDef =>
				         WeaponDefCanUseAmmoDef(def.defName, ammoDef.defName)))
			{
				if (def.descriptionHyperlinks == null)
				{
					def.descriptionHyperlinks = new List<DefHyperlink>();
				}

				def.descriptionHyperlinks.Add(ammoDef);
				ThingDef projectile = ammoDef.GetModExtension<AmmunitionExtension>()?.bulletDef;
				def.descriptionHyperlinks.Add(projectile ?? def.Verbs.FirstOrDefault()?.defaultProjectile);
			}
		}

		internal static void SaveAmmoDefault()
		{
			try
			{
				if (Settings.Settings.CategoryWeaponDictionary == null ||
				    Settings.Settings.ExemptionWeaponDictionary == null)
				{
					return;
				}

				string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammoDefault.lts");
				var file = new SaveFile()
				{
					Categories = Settings.Settings.CategoryWeaponDictionary,
					Exemptions = Settings.Settings.ExemptionWeaponDictionary
				};

				var bf = new BinaryFormatter();
				using (var stream =
				       new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				{
					bf.Serialize(stream, file);
				}

				string pathReadable = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammoLTSReadable.txt");
				using (var writer = new StreamWriter(pathReadable))
				{
					foreach (KeyValuePair<string, Dictionary<string, bool>> dictionary
					         in Settings.Settings.CategoryWeaponDictionary)
					{
						writer.WriteLine("Category: <" + dictionary.Key + ">");
						foreach (KeyValuePair<string, bool> weaponDef in dictionary.Value.Where(weaponDef =>
							         weaponDef.Value.Equals(true)))
						{
							writer.WriteLine("<" + weaponDef.Key + ">");
						}

						writer.WriteLine();
					}
				}
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error saving ammo defaults file! " + ex.Message);
				}
			}
		}

		internal static void LoadAmmoDefault()
		{
			try
			{
				string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammoDefault.lts");
				if (!File.Exists(path))
				{
					return;
				}

				SaveFile file;
				var bf = new BinaryFormatter();
				using (var stream = new FileStream(path, FileMode.Open))
				{
					stream.Seek(0, SeekOrigin.Begin);
					file = (SaveFile)bf.Deserialize(stream);
				}

				Settings.Settings.ExemptionWeaponDictionary = file.Exemptions;
				Settings.Settings.CategoryWeaponDictionary = file.Categories;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error loading ammo defaults file! " + ex.Message);
				}
			}
		}

		internal static void Save()
		{
			try
			{
				if (Settings.Settings.CategoryWeaponDictionary == null ||
				    Settings.Settings.ExemptionWeaponDictionary == null ||
				    Settings.Settings.BagSettingsDictionary == null)
				{
					return;
				}

				var file = new SaveFile()
				{
					Categories = Settings.Settings.CategoryWeaponDictionary,
					Exemptions = Settings.Settings.ExemptionWeaponDictionary,
					Bags = Settings.Settings.BagSettingsDictionary
				};

				string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammo.lts");
				var bf = new BinaryFormatter();
				using (var stream =
				       new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				{
					bf.Serialize(stream, file);
				}
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error saving ammo! " + ex.Message);
				}
			}
		}

		private static void Load()
		{
			try
			{
				string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammo.lts");
				if (!File.Exists(path))
				{
					return;
				}

				SaveFile file;
				var bf = new BinaryFormatter();
				using (var stream = new FileStream(path, FileMode.Open))
				{
					stream.Seek(0, SeekOrigin.Begin);
					file = (SaveFile)bf.Deserialize(stream);
				}

				Settings.Settings.ExemptionWeaponDictionary = file.Exemptions;
				Settings.Settings.CategoryWeaponDictionary = file.Categories;
				Settings.Settings.BagSettingsDictionary = file.Bags;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("Error loading ammo! " + ex.Message);
				}
			}
		}

		/// <summary>
		/// Checks whether a pawn have enough or need ammo for a weapon.
		/// </summary>
		/// <param name="pawn">The pawn user.</param>
		/// <param name="weapon">The weapon used.</param>
		/// <param name="kitComp">The kit being used.</param>
		/// <param name="consumeAmmo">Whether to consume ammo when checking or not.</param>
		/// <returns>returns true if ammo is not needed or is otherwise available.</returns>
		public static bool AmmoCheck(Pawn pawn, Thing weapon, out KitComponent kitComp, bool consumeAmmo)
		{
			kitComp = null;
			try
			{
				if (pawn.DestroyedOrNull() || weapon.DestroyedOrNull())
				{
					return true;
				}

				// Currently, Mechanoids and animals can't wear kits.
				if ((pawn.RaceProps.IsMechanoid /*&& !Settings.Settings.UseMechanoidAmmo*/) ||
				    (pawn.RaceProps.Animal /*&& !Settings.Settings.UseAnimalAmmo*/))
				{
					return true;
				}

				// Not every pawn can wear apparel, hence can't wear kits thus should be exempt.
				if (pawn.apparel == null)
				{
					return true;
				}

				if (IsExempt(weapon.def.defName))
				{
					return true;
				}

				//If all prior "fails", this means the weapon indeed need ammo.

				//If the pawn is naked, can't be wearing a kit.
				if (!pawn.apparel.AnyApparel)
				{
					return false;
				}

				List<Kit> kits = GetWornKits(pawn);
				if (!kits.Any())
				{
					return false;
				}

				foreach (Kit kit in kits)
				{
					kitComp = kit.KitComp;
					AmmoSlot ammoSlot = kitComp.Bags.FirstOrDefault(
						t => t.Use &&
						     t.ChosenAmmo != null &&
						     t.Count > 0 &&
						     WeaponDefCanUseAmmoDef(
							     weapon.def.defName, t.ChosenAmmo.defName));

					//Does this kit contain viable ammo in a slot?
					if (ammoSlot == null)
					{
						continue;
					}

					if (consumeAmmo)
					{
						if (pawn.IsColonist)
						{
							ammoSlot.Count--;
						}
						else if (Settings.Settings.NpcUseAmmo)
						{
							ammoSlot.Count--;
						}

					}

					kitComp.LastUsedAmmo = ammoSlot.ChosenAmmo;
					return true;
				}

				//No ammo available
				kitComp = null;
				return false;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					if (kitComp != null)
					{
						Log.Error(kitComp.parent.def.defName + "have components but not viable.");
						kitComp = null;
					}

					Log.Error("Failure in getUsableKitCompForWeapon: " + ex.Message);
				}
			}

			kitComp = null;
			return false;
		}

		/// <summary>
		/// If a weapon is monitored by this mod and if so, whether it is exempt or not.
		/// </summary>
		/// <param name="weaponDefName">The weapon def in question</param>
		/// <returns>true is this weapon can fire due to being exempt</returns>
		public static bool IsExempt(string weaponDefName)
		{
			return !Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weaponDefName, out bool exempt) || exempt;
		}

		private static bool IsViableWeaponDef(ThingDef weapon)
		{
			return !weapon.IsMeleeWeapon &&
			       !weapon.IsBuildingArtificial &&
			       !weapon.Verbs.Any(y => y.verbClass == typeof(Verb_ShootOneUse)) &&
			       weapon.HasComp(typeof(CompEquippable)) &&
			       weapon.IsWithinCategory(ThingCategoryDefOf.Weapons);
		}

		public static void FixCategoryWeaponDictionaries()
		{
			foreach (ThingDef weaponDef in AvailableProjectileWeapons)
			{
				bool hasAmmo = false;
				foreach (KeyValuePair<string, Dictionary<string, bool>> categoryDictionaries in
				         Settings.Settings.CategoryWeaponDictionary)
				{
					categoryDictionaries.Value.TryGetValue(weaponDef.defName, out bool needAmmo);
					if (needAmmo)
					{
						hasAmmo = true;
					}
				}

				if (!hasAmmo)
				{
					Settings.Settings.ExemptionWeaponDictionary.SetOrAdd(weaponDef.defName, true);
				}
				AmmoLogic.ResetHyperLinksForWeapon(weaponDef);
			}
		}

		public static void SetAllCategoriesForWeaponToNotRequireAmmo(string thingDefName)
		{
			
		}

		public static List<AmmoCategoryDef> AvailableAmmoForWeapon(string weaponDefName)
		{
			var list = new List<AmmoCategoryDef>();
			var ammoCatList = AmmoCategoryDefs.ToList();
			for (int i = 0; i < ammoCatList.Count(); i++)
			{
				if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(ammoCatList[i].defName,
					    out Dictionary<string, bool> dic))
				{
					continue;
				}

				if (!dic.TryGetValue(weaponDefName, out bool res))
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

		public static bool WeaponDefCanUseAmmoDef(string weaponDefName, string ammoDefName)
		{
			if (!_ammoToCategoriesDictionary.TryGetValue(ammoDefName, out List<AmmoCategoryDef> availableCategories) ||
			    !availableCategories.Any())
			{
				return false;
			}

			for (int c = 0; c < availableCategories.Count(); c++)
			{
				if (!Settings.Settings.CategoryWeaponDictionary.TryGetValue(availableCategories[c].defName,
					    out Dictionary<string, bool> dic))
				{
					continue;
				}

				if (dic.TryGetValue(weaponDefName, out bool res) && res)
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