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

        //public static List<AmmoCategoryDef> NoneCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> PrimitiveCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> MedievalCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> IndustrialCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> ExplosiveCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> SpacerCats = new List<AmmoCategoryDef>();
        //public static List<AmmoCategoryDef> ArchotechCats = new List<AmmoCategoryDef>();

        public static List<ThingDef> NoneAmmo = new List<ThingDef>();
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
        internal static List<ThingDef> AmmoTypesList(AmmoTypes type) {
            switch (type) {
                case AmmoTypes.None:
                    return NoneAmmo;
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
        public static bool CanBeLootedByColony(Thing thing) {
            if (!(thing is IStrippable strippable)) {
                return false;
            }
            if (!strippable.AnythingToStrip()) {
                return false;
            }
            if (thing is Things.Kit kit) {
                foreach (var item in kit.KitComp.Bags) {
                    if (item.Count > 0) {
                        return true;
                    }
                }
            }
            Pawn pawn = thing as Pawn ?? (thing as Corpse).InnerPawn;
            if (pawn == null) {
                return false;
            }
            if (pawn.IsQuestLodger()) {
                return false;
            }
            if (GetWornKits(pawn).Where(x => x.KitComp.Bags.Where(y => y.Count > 0).Count() > 0).Count() < 1) {
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
            Pawn pawn = thing as Pawn ?? (thing as Corpse).InnerPawn;
            if (pawn != null) {
                List<Things.Kit> kits = GetWornKits(pawn)?.ToList();
                if (kits != null && kits.Count > 0) {
                    foreach (Things.Kit kit in kits) {
                        for (int i = 0; i < kit.KitComp.Props.bags; i++) {
                            if (kit.KitComp.Bags[i].Count > 0) {
                                DebugThingPlaceHelper.DebugSpawn(kit.KitComp.Bags[i].ChosenAmmo, kit.SpawnedParentOrMe.PositionHeld, kit.KitComp.Bags[i].Count);
                                kit.KitComp.Bags[i].Count = 0;
                            }
                        }
                    }
                }
            }
        }
        public static void EquipPawn(Pawn p) {
            try {
                if (p.apparel != null) {
                    Things.Kit app;
                    //Removes all spawned kits
                    if (p.apparel.WornApparelCount > 1) {
                        app = (Things.Kit)p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
                        while (app != null) {
                            p.apparel.Remove(app);
                            app.Destroy();
                            app = null;
                            app = (Things.Kit)p.apparel.WornApparel.FirstOrDefault(x => x.TryGetComp<KitComponent>() != null);
                        }
                    }
                    if (p.equipment != null && p.equipment.Primary != null) {
                        IEnumerable<AmmoCategoryDef> categories = AmmoCategoryDefs.Where(x => Settings.Settings.CategoryWeaponDictionary.TryGetValue(x.defName, out Dictionary<string, bool> wep) == true && wep.TryGetValue(p.equipment.Primary.def.defName, out bool res) && res);
                        if (categories.Count() > 0) {
                            ThingDef kit = AvailableKits.RandomElement();
                            if (kit != null) {
                                app = (Things.Kit)ThingMaker.MakeThing(kit, GenStuff.AllowedStuffsFor(kit, TechLevel.Undefined).RandomElement());
                                if (app != null) {
                                    p.apparel.Wear(app);
                                    for (int i = 0; i < app.KitComp.Props.bags; i++) {
                                        AmmoCategoryDef ammoCatDef = categories.RandomElement();
                                        string ammoDef = ammoCatDef.ammoDefs.RandomElement();
                                        app.KitComp.Bags[i].ChosenAmmo = AvailableAmmo.FirstOrDefault(x => x.defName == ammoDef);
                                        if (app.KitComp.Bags[i].ChosenAmmo != null) {
                                            app.KitComp.Bags[i].MaxCount = app.KitComp.Props.ammoCapacity[i];
                                            app.KitComp.Bags[i].Count = Rand.Range(app.KitComp.Props.ammoCapacity[i] / 3, app.KitComp.Props.ammoCapacity[i]);
                                        }
                                        else {
                                            Log.Message("There are somehow no ammo within randomly chosen category.");
                                            app.Destroy();
                                        }
                                    }
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
        internal static void Initialize(bool load = true) {
            try {
                if (load)
                    Load();
                foreach (AmmoCategoryDef category in AmmoCategoryDefs) {
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
                            dic.Add(weapon.defName, category.autoAssignable && !category.excludeWeaponDefs.Contains(weapon.defName) && (category.includeWeaponDefs.Contains(weapon.defName) || TechLevelEqualCategory(weapon.techLevel, category.ammoType)));
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
        /// <summary>
        /// Checks whether a pawn have enough or need ammo for a weapon.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="weapon"></param>
        /// <param name="ammo"></param>
        /// <returns>true if ammo is available or not needed</returns>
        public static bool AmmoCheck(Pawn pawn, Thing weapon, out KitComponent kitComp, bool consumeAmmo) {
            kitComp = null;
            try {
                if (pawn.DestroyedOrNull() || weapon.DestroyedOrNull())
                    return true;
                if ((pawn.RaceProps.IsMechanoid && !Settings.Settings.UseMechanoidAmmo) || (pawn.RaceProps.Animal && !Settings.Settings.UseAnimalAmmo))
                    return true;
                Settings.Settings.ExemptionWeaponDictionary.TryGetValue(weapon.def.defName, out bool exempt);
                if (exempt)
                    return true;
                //If all prior fails, this means the weapon needs ammo.
                if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0) {
                    List<Things.Kit> kits = GetWornKits(pawn);
                    if (kits.Any()) {
                        foreach (Things.Kit kit in kits) {
                            kitComp = kit.KitComp;
                            for (int i = 0; i < kitComp.Props.bags; i++) {
                                if (kitComp.Bags[i].Use && kitComp.Bags[i].Count > 0 && kitComp.Bags[i].ChosenAmmo != null) {
                                    string defName = kitComp.Bags[i].ChosenAmmo.defName;
                                    IEnumerable<AmmoCategoryDef> ammoCatList = AmmoCategoryDefs.Where(x => x.ammoDefs.Contains(defName));
                                    if (ammoCatList.Any()) {
                                        foreach (AmmoCategoryDef ammoCatDef in ammoCatList) {
                                            if (Settings.Settings.CategoryWeaponDictionary.TryGetValue(ammoCatDef.defName, out Dictionary<string, bool> dic)) {
                                                if (dic.TryGetValue(weapon.def.defName, out bool res)) {
                                                    if (res) {
                                                        if (consumeAmmo) {
                                                            kitComp.Bags[i].Count--;
                                                        }
                                                        kitComp.LastUsedAmmo = kitComp.Bags[i].ChosenAmmo;
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else {
                                        Log.Error("Found ammo without a category, why is this?!");
                                    }
                                }
                            }
                        }
                    }
                }
                kitComp = null;
            }
            catch (Exception ex) {
                if (kitComp != null) {
                    Log.Error(kitComp.parent.def.defName + "have components but not viable.");
                }
                Log.Error("Failure in getUsableKitCompForWeapon: " + ex.Message);
            }
            return false;
        }

        public static List<Things.Kit> GetWornKits(Pawn pawn) {
            List<Things.Kit> kits = new List<Things.Kit>();
            foreach (Apparel app in pawn.apparel.WornApparel.Where(x => x.TryGetComp<KitComponent>() != null)) {
                kits.Add(app as Things.Kit);
            }
            return kits;
        }


    }
}
