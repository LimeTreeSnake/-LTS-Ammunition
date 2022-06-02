using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System;
using Ammunition.Defs;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Ammunition.Components;
using UnityEngine;

namespace Ammunition.Logic {

    [StaticConstructorOnStartup]
    static class AmmoLogic {

        /// <summary>
        /// Returns all equipable weapons that fires projectiles.
        /// </summary>
        public static IEnumerable<ThingDef> AvailableProjectileWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsWeaponUsingProjectiles && x.HasComp(typeof(CompEquippable)) && x.IsWithinCategory(ThingCategoryDefOf.Weapons));
        /// <summary>
        /// Returns all thingdefs with an ammunition component.
        /// </summary>
        public static IEnumerable<ThingDef> AvailableAmmo = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.HasComp(typeof(Components.AmmunitionComponent)));
        /// <summary>
        /// Returns all thingdefs with a kit component.
        /// </summary>
        public static IEnumerable<ThingDef> AvailableKits = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.HasComp(typeof(Components.KitComponent)));

        
        public static IEnumerable<AmmoCategoryDef> AmmoCategoryDefs => DefDatabase<AmmoCategoryDef>.AllDefsListForReading;

        public static Dictionary<string, List<ThingDef>> WeaponAmmoList = new Dictionary<string, List<ThingDef>>();

        public static List<AmmoCategoryDef> PrimitiveCats = new List<AmmoCategoryDef>();
        public static List<AmmoCategoryDef> MedievalCats = new List<AmmoCategoryDef>();
        public static List<AmmoCategoryDef> IndustrialCats = new List<AmmoCategoryDef>();
        public static List<AmmoCategoryDef> ExplosiveCats = new List<AmmoCategoryDef>();
        public static List<AmmoCategoryDef> SpacerCats = new List<AmmoCategoryDef>();
        public static List<AmmoCategoryDef> ArchotechCats = new List<AmmoCategoryDef>();

        public static List<ThingDef> PrimitiveAmmo = new List<ThingDef>();
        public static List<ThingDef> MedievalAmmo = new List<ThingDef>();
        public static List<ThingDef> IndustrialAmmo = new List<ThingDef>();
        public static List<ThingDef> ExplosiveAmmo = new List<ThingDef>();
        public static List<ThingDef> SpacerAmmo = new List<ThingDef>();
        public static List<ThingDef> ArchotechAmmo = new List<ThingDef>();

        internal static bool TechLevelEqualCategory(TechLevel tech, AmmoTypes category) {
            switch (tech) {
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
        internal static List<AmmoCategoryDef> CategoryTypesList(AmmoTypes type) {
            switch (type) {
                case AmmoTypes.None:
                    break;
                case AmmoTypes.Primitive:
                    return PrimitiveCats;
                case AmmoTypes.Medieval:
                    return MedievalCats;
                case AmmoTypes.Industrial:
                    return IndustrialCats;
                case AmmoTypes.Explosive:
                    return ExplosiveCats;
                case AmmoTypes.Spacer:
                    return SpacerCats;
                case AmmoTypes.Archotech:
                    return ArchotechCats;
                default:
                    break;
            }
            return null;
        }
        internal static List<ThingDef> AmmoTypesList(AmmoTypes type) {
            switch (type) {
                case AmmoTypes.None:
                    break;
                case AmmoTypes.Primitive:
                    return PrimitiveAmmo;
                case AmmoTypes.Medieval:
                    return MedievalAmmo;
                case AmmoTypes.Industrial:
                    return IndustrialAmmo;
                case AmmoTypes.Explosive:
                    return ExplosiveAmmo;
                case AmmoTypes.Spacer:
                    return SpacerAmmo;
                case AmmoTypes.Archotech:
                    return ArchotechAmmo;
                default:
                    break;
            }
            return null;
        }

        internal static IEnumerable<ThingDef> GetKitTypesList(AmmoTypes type) {
            return AvailableKits.Where(x => x.GetCompProperties<CompProps_Kit>().kitCategory == type);
        }
        public static bool CanBeLootedByColony(Thing thing) {
            IStrippable strippable = thing as IStrippable;
            if (strippable == null) {
                return false;
            }
            if (!strippable.AnythingToStrip()) {
                return false;
            }
            Pawn pawn = thing as Pawn == null ? (thing as Corpse).InnerPawn : thing as Pawn;
            if (pawn == null) {
                return false;
            }
            if (pawn.IsQuestLodger()) {
                return false;
            }
            if (GetWornKitComps(pawn).Where(x => x.Count > 0).Count() < 1) {
                return false;
            }
            if (pawn.Downed || pawn.Dead) {
                return true;
            }
            if (pawn.IsPrisonerOfColony && pawn.guest.PrisonerIsSecure) {
                return true;
            }
            return false;
        }
        public static void LootAmmo(Thing thing) {
            Pawn pawn = thing as Pawn == null ? (thing as Corpse).InnerPawn : thing as Pawn;
            if (pawn != null) {
                List<KitComponent> kits = GetWornKitComps(pawn)?.ToList();
                if (kits != null && kits.Count > 0) {
                    foreach (KitComponent kit in kits) {
                        DebugThingPlaceHelper.DebugSpawn(kit.ChosenAmmo, kit.parent.PositionHeld, kit.Count);
                        kit.Count = 0;
                    }
                }
            }
        }

        internal static void Initialize(bool load = true) {
            try {
                if (load)
                    Load();
                foreach (ThingDef kit in AvailableKits) {
                }
                foreach (AmmoCategoryDef category in AmmoCategoryDefs) {
                    CategoryTypesList(category.ammoType)?.Add(category);
                    List<ThingDef> ammoList = AmmoTypesList(category.ammoType);
                    foreach (string ammoStringDef in category.ammoDefs) {
                        ThingDef ammoDef = AvailableAmmo.FirstOrDefault(x => x.defName == ammoStringDef);
                        if (ammoDef != null)
                            ammoList.Add(ammoDef);
                    }
                    if (!Settings.Settings.CategoryWeaponDictionary.ContainsKey(category.defName)) {
                        Settings.Settings.CategoryWeaponDictionary.Add(category.defName, new Dictionary<string, bool>());
                    }
                    Settings.Settings.CategoryWeaponDictionary.TryGetValue(category.defName, out Dictionary<string, bool> dic);
                    if (dic == null) {
                        Settings.Settings.CategoryWeaponDictionary.SetOrAdd(category.defName, new Dictionary<string, bool>());
                    }
                    foreach (ThingDef weapon in AvailableProjectileWeapons) {
                        if (!dic.ContainsKey(weapon.defName)) {
                            dic.Add(weapon.defName, !category.excludeWeaponDefs.Contains(weapon.defName) && (category.includeWeaponDefs.Contains(weapon.defName) || TechLevelEqualCategory(weapon.techLevel, category.ammoType)));
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Message("Error initializing Ammunition Framework: " + ex.Message);
            }
        }
        internal static void ResetInitialize() {
            Settings.Settings.CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
            Initialize(false);
        }
        internal static void ResetInitialize(AmmoCategoryDef def) {

        }
        internal static void Save() {
            try {
                if (Settings.Settings.CategoryWeaponDictionary == null || Settings.Settings.ExemptionWeaponDictionary == null)
                    return;
                SaveFile file = new SaveFile() {
                    Categories = Settings.Settings.CategoryWeaponDictionary,
                    Exemptions = Settings.Settings.ExemptionWeaponDictionary
                };
                string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammo.lts");
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                    bf.Serialize(stream, file);
                }
            }
            catch (Exception ex) {
                Log.Error("Error saving ammo! " + ex.Message);
            }
        }
        internal static void Load() {
            try {
                string path = Path.Combine(GenFilePaths.ConfigFolderPath + "/ammo.lts");
                if (File.Exists(path)) {
                    SaveFile file;
                    BinaryFormatter bf = new BinaryFormatter();
                    using (FileStream stream = new FileStream(path, FileMode.Open)) {
                        stream.Seek(0, SeekOrigin.Begin);
                        file = (SaveFile)bf.Deserialize(stream);
                    }
                    Settings.Settings.ExemptionWeaponDictionary = file.Exemptions;
                    Settings.Settings.CategoryWeaponDictionary = file.Categories;
                }
            }
            catch (Exception ex) {
                Log.Error("Error loading ammo! " + ex.Message);
            }
        }

        public static void ConsumeAmmo(Pawn pawn, Thing weapon) {
            getWornKitForWeaponWithAmmo(pawn, weapon, out KitComponent kit);
            if (kit != null) {
                kit.Count--;
            }
        }
        public static void EquipPawn(Pawn p) {
            try {
                if (p.apparel != null) {
                    Apparel app;
                    //Removes all spawned kits
                    if (p.apparel.WornApparelCount > 1) {
                        app = p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
                        while (app != null) {
                            p.apparel.Remove(app);
                            app.Destroy();
                            app = null;
                            app = p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
                            Log.Message("Destroyed apparel kit");
                        }
                    }
                    if (p.equipment != null && p.equipment.Primary != null) {
                        IEnumerable<AmmoCategoryDef> categories = AmmoCategoryDefs.Where(x => Settings.Settings.CategoryWeaponDictionary.TryGetValue(x.defName, out Dictionary<string, bool> wep) == true && wep.TryGetValue(p.equipment.Primary.def.defName, out bool res) && res);
                        if (categories.Count() > 0) {
                            AmmoCategoryDef ammoCatDef = categories.RandomElement();
                            ThingDef kit = GetKitTypesList(ammoCatDef.ammoType)?.RandomElement();
                            if (kit != null) {
                                app = (Apparel)ThingMaker.MakeThing(kit, GenStuff.AllowedStuffsFor(kit, TechLevel.Undefined).RandomElement());
                                KitComponent comp = app.GetComp<KitComponent>();
                                string ammoDef = ammoCatDef.ammoDefs.RandomElement();
                                comp.ChosenAmmo = AvailableAmmo.FirstOrDefault(x => x.defName == ammoDef);
                                if (comp.ChosenAmmo != null) {
                                    comp.MaxCount = comp.Props.ammoCapacity;
                                    comp.Count = Rand.Range(comp.Props.ammoCapacity / 3, comp.Props.ammoCapacity);
                                    p.apparel.Wear(app);
                                }
                                else {
                                    Log.Message("There are somehow no ammo within randomly chosen category.");
                                    app.Destroy();
                                }
                            }
                            else {
                                Log.Message("There are no kits satisfying the required ammo.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Message("EquipPawn error! - " + ex.Message);
            }
        }
        /// <summary>
        /// Checks whether a pawn have enough ammo for a weapon.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="weapon"></param>
        /// <param name="ammo"></param>
        /// <returns>true if ammo is available or not needed</returns>
        public static bool AmmoCheck(Pawn pawn, Thing weapon, out KitComponent kit) {
            kit = null;
            if (!pawn.DestroyedOrNull() && !weapon.DestroyedOrNull()) {
                if ((pawn.RaceProps.IsMechanoid && !Settings.Settings.UseMechanoidAmmo) || (pawn.RaceProps.Animal && !Settings.Settings.UseAnimalAmmo) || pawn.apparel == null)
                    return true;
                Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.def.defName, out bool exempt);
                if (exempt)
                    return true;
                getWornKitForWeaponWithAmmo(pawn, weapon, out kit);
                if (kit == null)
                    return false;
                if (kit.Count < 1)
                    return false;
            }
            return true;
        }
        public static void getWornKitForWeaponWithAmmo(Pawn pawn, Thing weapon, out KitComponent comp) {
            comp = null;
            try {
                if (pawn == null || weapon == null)
                    return;
                Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.def.defName, out bool exempt);
                if (exempt)
                    return;
                if (pawn.apparel.WornApparelCount > 0) {
                    foreach (Apparel app in pawn.apparel.WornApparel) {
                        comp = app.TryGetComp<KitComponent>();
                        if (comp == null)
                            continue;
                        if (comp.Count > 0 && comp.ChosenAmmo != null) {
                            string defName = comp.ChosenAmmo.defName;
                            List<AmmoCategoryDef> list = CategoryTypesList(comp.Props.kitCategory).Where(x => x.ammoDefs.Count > 0 && x.ammoDefs.Contains(defName)).ToList();
                            if (list.Count > 0) {
                                foreach (AmmoCategoryDef item in list) {
                                    if (Settings.Settings.CategoryWeaponDictionary.TryGetValue(item.defName, out Dictionary<string, bool> dic)) {
                                        if (dic.TryGetValue(weapon.def.defName, out bool res)) {
                                            if (res) {
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        comp = null;
                    }
                }
            }
            catch (Exception ex) {
                if (comp != null) {
                    Log.Error(comp.ChosenAmmo.defName + "-" + comp.parent.def.defName + "-" + CategoryTypesList(comp.Props.kitCategory));
                }
                Log.Error("Failure in getWornKitForWeaponWithAmmo: " + ex.Message);
            }
        }

        public static IEnumerable<Apparel> GetWornKits(Pawn pawn) {
            return pawn.apparel.WornApparel.Where(x => x.TryGetComp<KitComponent>() != null);
        }
        public static IEnumerable<KitComponent> GetWornKitComps(Pawn pawn) {
            return pawn.apparel.WornApparel.Where(x => x.TryGetComp<KitComponent>() != null).Select(t => t.GetComp<KitComponent>());
        }
    }
}
