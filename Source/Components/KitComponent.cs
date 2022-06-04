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
        #endregion Fields
        #region Properties
        public List<Bag> Bags {
            get {
                return bags;
            }
        }
        public override void Initialize(CompProperties props) {
            base.Initialize(props);
            CompProps_Kit compProps_Kit = props as CompProps_Kit;
            for (int i = 0; i < compProps_Kit.bags; i++) {
                Bag bag = new Bag();
                bag.ChosenAmmo = AmmoLogic.AvailableAmmo.FirstOrDefault();
                bag.Count = 0;
                bag.MaxCount = compProps_Kit.ammoCapacity[i];
                bag.Capacity = compProps_Kit.ammoCapacity[i];
                bag.Use = true;
                bags.Add(bag);
            }
        }
        public CompProps_Kit Props => (CompProps_Kit)props;
        #endregion Properties
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Collections.Look(ref bags, "Bags", LookMode.Deep);
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
