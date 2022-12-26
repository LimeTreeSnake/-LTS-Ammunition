using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using Ammunition.Logic;
using Ammunition.Models;
using Ammunition.DefModExtensions;
using Ammunition.Defs;

namespace Ammunition.Components {
    public class KitComponent : ThingComp {
        #region Fields
        private List<Bag> bags = new List<Bag>();
        private ThingDef lastUsedAmmo;
        #endregion Fields
        #region Properties
        public List<Bag> Bags {
            get {
                return bags;
            }
        }
        public CompProps_Kit Props => (CompProps_Kit)props;
        public ThingDef LastUsedAmmo {
            get => lastUsedAmmo;
            set => lastUsedAmmo = value;
        }
        public ThingDef LastUsedBullet => LastUsedAmmo?.GetModExtension<AmmunitionExtension>()?.bulletDef;
        #endregion Properties
        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            InitializeBags();
        }
        public void InitializeBags() {
            if (bags == null) {
                bags = new List<Bag>();
            }
            if (Settings.Settings.BagSettingsDictionary.TryGetValue(parent.def.defName, out List<int> bagSettings)) {
                for (int i = 0; i < bagSettings.Count; i++) {
                    Bag bag = new Bag {
                        ChosenAmmo = Settings.Settings.InitialAmmoType == null ? Settings.Settings.GetDefaultAmmo() : Settings.Settings.InitialAmmoType,
                        Count = 0,
                        Capacity = bagSettings[i],
                        MaxCount = bagSettings[i],
                        Use = true
                    };
                    bags.Add(bag);
                }
            }
            else {
                CompProps_Kit compProps_Kit = props as CompProps_Kit;
                for (int i = 0; i < compProps_Kit.ammoCapacity.Count; i++) {
                    Bag bag = new Bag {
                        ChosenAmmo = Settings.Settings.InitialAmmoType == null ? Settings.Settings.GetDefaultAmmo() : Settings.Settings.InitialAmmoType,
                        Count = 0,
                        Capacity = compProps_Kit.ammoCapacity[i],
                        MaxCount = compProps_Kit.ammoCapacity[i],
                        Use = true
                    };
                    bags.Add(bag);
                }
            }
        }
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Collections.Look(ref bags, "Bags", LookMode.Deep);
        }

        public override void Notify_Unequipped(Pawn pawn) {
            base.Notify_Unequipped(pawn);
            if (pawn.Spawned) {
                Unload(pawn.PositionHeld);
            }
        }
        public override void Notify_Equipped(Pawn pawn) {
            base.Notify_Equipped(pawn);
            if (pawn.Spawned && pawn.equipment?.Primary != null) {
                if (AmmoLogic.IsExempt(pawn.equipment.Primary.def.defName)) {
                    return;
                }
                else {
                    if (Settings.Settings.InitialAmmoType != null && AmmoLogic.WeaponDefCanUseAmmoDef(pawn.equipment.Primary.def.defName, Settings.Settings.InitialAmmoType.defName)) {
                        for (int i = 0; i < bags.Count; i++) {
                            bags[i].ChosenAmmo = Settings.Settings.InitialAmmoType;
                        }
                    }
                    else {
                        ThingDef ammoDef = null;
                        List<AmmoCategoryDef> ammoCategories = AmmoLogic.AvailableAmmoForWeapon(pawn.equipment.Primary.def.defName);
                        if (ammoCategories != null && ammoCategories.Any()) {
                            string ammoDefName = ammoCategories.RandomElement().ammoDefs.RandomElement();
                            ammoDef = AmmoLogic.AvailableAmmo.FirstOrDefault(x => x.defName == ammoDefName);
                            if (ammoDef != null) {
                                for (int i = 0; i < bags.Count; i++) {
                                    bags[i].ChosenAmmo = ammoDef;
                                }
                            }
                        }

                    }
                }
            }
        }
        public void Unload(IntVec3 pos) {
            foreach (Bag bag in bags) {
                if (bag.Count > 0) {
                    DebugThingPlaceHelper.DebugSpawn(bag.ChosenAmmo, pos, bag.Count);
                    bag.Count = 0;
                }
            }
        }
    }

    public class CompProps_Kit : CompProperties {
        public List<int> ammoCapacity;
        public bool canBeGenerated = true;
        public CompProps_Kit() {
            compClass = typeof(KitComponent);
        }
    }
}
