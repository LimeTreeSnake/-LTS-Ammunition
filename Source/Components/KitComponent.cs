using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using Ammunition.Logic;
using Ammunition.Models;

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
        public ThingDef LastUsedBullet => LastUsedAmmo?.GetCompProperties<CompProps_Ammunition>()?.bulletDef;
        #endregion Properties
        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            CompProps_Kit compProps_Kit = props as CompProps_Kit;
            if (bags == null) {
                bags = new List<Bag>();
            }
            for (int i = 0; i < compProps_Kit.bags; i++) {
                Bag bag = new Bag {
                    ChosenAmmo = AmmoLogic.AvailableAmmo?.FirstOrDefault(),
                    Count = 0,
                    MaxCount = compProps_Kit.ammoCapacity[i],
                    Capacity = compProps_Kit.ammoCapacity[i],
                    Use = true
                };
                bags.Add(bag);
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
        public int bags;
        public CompProps_Kit() {
            compClass = typeof(KitComponent);
        }
    }
}
