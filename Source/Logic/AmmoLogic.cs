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
using System.Text;

namespace Ammunition.Logic
{

	[StaticConstructorOnStartup]
	static class AmmoLogic
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
					return category == AmmoTypes.Spacer;


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
				foreach (Bag t in kit.KitComp.Bags.Where(t => t.Count > 0))
				{
					DebugThingPlaceHelper.DebugSpawn(t.ChosenAmmo,
						kit.SpawnedParentOrMe.PositionHeld, t.Count);

					t.Count = 0;
				}
			}
		}

		private static void LoadSpawnedKit(Kit apparel, AmmoCategoryDef def, Pawn pawn = null)
		{
			for (int i = 0; i < apparel.KitComp.Bags.Count; i++)
			{
				string ammoDef = def.ammoDefs.RandomElement();
				apparel.KitComp.Bags[i].ChosenAmmo = AvailableAmmo.FirstOrDefault(x => x.defName == ammoDef);
				if (apparel.KitComp.Bags[i].ChosenAmmo != null)
				{
					apparel.KitComp.Bags[i].MaxCount = apparel.KitComp.Props.ammoCapacity[i];
					apparel.KitComp.Bags[i].Count =
						(int)(Rand.Range(apparel.KitComp.Props.ammoCapacity[i] / 3,
							      apparel.KitComp.Props.ammoCapacity[i]) *
						      Settings.Settings.PawnAmmoSpawnRate);

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
				if (p.apparel == null)
				{
					return;
				}

				if ((p.RaceProps.IsMechanoid /*&& !Settings.Settings.UseMechanoidAmmo*/) ||
				    (p.RaceProps.Animal /*&& !Settings.Settings.UseAnimalAmmo*/))
				{
					return;
				}

				Kit apparel = null;
				if (p.apparel.WornApparelCount > 0)
				{
					apparel = (Kit)p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
				}

				if (p.equipment?.Primary == null)
				{
					return;
				}

				IEnumerable<AmmoCategoryDef> categories = AmmoCategoryDefs.Where(x =>
						Settings.Settings.CategoryWeaponDictionary.TryGetValue
							(x.defName, out Dictionary<string, bool> wep) &&
						wep != null &&
						wep.TryGetValue(p.equipment.Primary.def.defName, out bool res) &&
						res)
					.ToList();

				if (!categories.Any())
				{
					return;
				}

				if (apparel != null)
				{
					LoadSpawnedKit(apparel, categories.RandomElement(), p);
					return;
				}

				//If not already wearing kit.
				var sorted = AvailableKits
					.Where(y => y.GetCompProperties<CompProps_Kit>().canBeGenerated &&
					            p.apparel.CanWearWithoutDroppingAnything(y))
					.OrderBy(x => x.GetCompProperties<CompProps_Kit>().ammoCapacity.Sum())
					.ToList();

				ThingDef kit = null;
				if (!p.ageTracker.Adult)
				{
					kit = sorted.Where(t => t.apparel.developmentalStageFilter.HasFlag
							(DevelopmentalStage.Child))
						.RandomElement();
				}
				else if (Settings.Settings.UseAmmoPerBullet &&
				         p.equipment.Primary.def.Verbs.FirstOrDefault
					         (x => x.verbClass == typeof(Verb_LaunchProjectile)) !=
				         null)
				{
					int burstCount = p.equipment.Primary.def.Verbs
						.FirstOrDefault(x => x.verbClass == typeof(Verb_LaunchProjectile))
						.burstShotCount;

					if (burstCount > 1)
					{
						kit = sorted.FirstOrDefault(y =>
							y.GetCompProperties<CompProps_Kit>().ammoCapacity.Sum() / burstCount > 20);
					}
				}
				else
				{
					for (int i = 0; i < sorted.Count(); i++)
					{
						if (!Rand.Bool)
						{
							continue;
						}

						kit = sorted[i];
						break;
					}
				}

				if (kit == null)
				{
					kit = sorted.First();
				}

				apparel = (Kit)ThingMaker.MakeThing(kit,
					GenStuff.AllowedStuffsFor(kit).RandomElement());

				if (apparel == null)
				{
					return;
				}

				p.apparel.Wear(apparel);
				LoadSpawnedKit(apparel, categories.RandomElement(), p);
			}
			catch (Exception ex)
			{
				Log.Error("LTS_EquipPawn error, contact LTS with this! - " + ex.Message);
			}
		}

		internal static void Initialize(bool load = true)
		{
			try
			{
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
					}

					Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName,
						out Dictionary<string, bool> dic);

					if (dic == null)
					{
						Settings.Settings.CategoryWeaponDictionary.SetOrAdd(category.defName,
							new Dictionary<string, bool>());
					}

					foreach (ThingDef weapon in AvailableProjectileWeapons)
					{
						if (dic != null && !dic.ContainsKey(weapon.defName))
						{
							bool assigned = (category.includeWeaponDefs.Contains(weapon.defName) ||
							                 (category.autoAssignable &&
							                  TechLevelEqualCategory(weapon.techLevel, category.ammoType)));

							dic.Add(weapon.defName, assigned && !category.excludeWeaponDefs.Contains(weapon.defName));
						}

						if (!Settings.Settings.ExemptionWeaponDictionary.ContainsKey(weapon.defName))
						{
							Settings.Settings.ExemptionWeaponDictionary.Add(weapon.defName,
								weapon.HasModExtension<ExemptAmmoUsageExtension>());
						}
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
				}

				Settings.Settings.GetDefaultAmmo();
				ResetHyperlinks();
			}
			catch (Exception ex)
			{
				Log.Error("Error initializing Ammunition Framework: " + ex.Message);
			}
		}

		private static void GenerateLists()
		{
			AmmoCategoryDefs = DefDatabase<AmmoCategoryDef>.AllDefsListForReading;
			AvailableProjectileWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x =>
					x.IsWeaponUsingProjectiles &&
					x.HasComp(typeof(CompEquippable)) &&
					x.IsWithinCategory(ThingCategoryDefOf.Weapons) &&
					!x.IsApparel &&
					!x.IsBuildingArtificial)
				.ToList();

			AvailableAmmo = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(x => x.HasModExtension<AmmunitionExtension>())
				.ToList();

			AvailableKits = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.HasComp(typeof(KitComponent)))
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

			foreach (ThingDef ammoDef in AvailableAmmo.Where(ammoDef =>
				         WeaponDefCanUseAmmoDef(def.defName, ammoDef.defName)))
			{
				if (def.descriptionHyperlinks == null)
				{
					def.descriptionHyperlinks = new List<DefHyperlink>();
				}

				def.descriptionHyperlinks.Add(ammoDef);
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
				Log.Error("Error saving ammo! " + ex.Message);
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
				Log.Error("Error loading ammo! " + ex.Message);
			}
		}

		/// <summary>
		/// Checks whether a pawn have enough or need ammo for a weapon.
		/// </summary>
		/// <param name="pawn">The pawn user.</param>
		/// <param name="weapon">The weapon used.</param>
		/// <param name="kitComp">The kit being used.</param>
		/// <param name="consumeAmmo">Whether to consume ammo when checking or not.</param>
		/// <returns>true if ammo is available or not needed.</returns>
		public static bool AmmoCheck(Pawn pawn, Thing weapon, out KitComponent kitComp, bool consumeAmmo)
		{
			kitComp = null;
			try
			{
				if (pawn.DestroyedOrNull() || weapon.DestroyedOrNull() || pawn.apparel == null)
				{
					return true;
				}

				if (weapon.def.IsMeleeWeapon || !AmmoCategoryDefs.Any())
				{
					return true;
				}

				if ((pawn.RaceProps.IsMechanoid /*&& !Settings.Settings.UseMechanoidAmmo*/) ||
				    (pawn.RaceProps.Animal /*&& !Settings.Settings.UseAnimalAmmo*/))
				{
					return true;
				}

				if (IsExempt(weapon.def.defName))
				{
					return true;
				}

				//If all prior "fails", this means the weapon indeed need ammo.
				if (pawn.apparel.WornApparelCount <= 0)
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
					foreach (Bag t in kitComp.Bags.Where(t => t.Use &&
					                                          t.ChosenAmmo != null &&
					                                          t.Count > 0)
						         .Where(t => WeaponDefCanUseAmmoDef(weapon.def.defName, t.ChosenAmmo.defName)))
					{
						if (consumeAmmo)
						{
							t.Count--;
						}

						kitComp.LastUsedAmmo = t.ChosenAmmo;
						return true;
					}
				}

				kitComp = null;

				return false;
			}
			catch (Exception ex)
			{
				if (kitComp != null)
				{
					Log.Error(kitComp.parent.def.defName + "have components but not viable.");
				}

				Log.Error("Failure in getUsableKitCompForWeapon: " + ex.Message);
			}

			kitComp = null;
			return false;
		}

		public static bool IsExempt(string weaponDefName)
		{
			if (Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weaponDefName, out bool exempt))
			{
				return exempt;
			}

			return true;
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