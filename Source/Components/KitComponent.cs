using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using UnityEngine;
using Ammunition.Logic;
using Ammunition.Defs;
using Ammunition.Gizmos;

namespace Ammunition.Components
{
    public class KitComponent : ThingComp
    {
        #region Fields
        private int count = 0;
        private int maxCount = 0;
        private ThingDef chosenAmmo;
        #endregion Fields
        public Pawn Wearer
        {
            get
            {
                Pawn_ApparelTracker pawn_ApparelTracker = this.ParentHolder as Pawn_ApparelTracker;
                if (pawn_ApparelTracker != null)
                {
                    return pawn_ApparelTracker.pawn;
                }
                return null;
            }
        }
        #region Properties
        public int Count
        {
            get
            {
                //if (maxCount < count)
                //{
                //    DebugThingPlaceHelper.DebugSpawn(ChosenAmmo, parent.PositionHeld, count - MaxCount);
                //    count = maxCount;
                //}
                return count;
            }
            set { count = value; }
        }
        public int MaxCount
        {
            get
            {
                if (maxCount > Props.ammoCapacity || maxCount < 0)
                    maxCount = Props.ammoCapacity;
                return maxCount;
            }
            set { maxCount = value; }
        }
        public ThingDef ChosenAmmo
        {
            get { return chosenAmmo; }
            set
            {
                if (value != chosenAmmo && Count > 0)
                {
                    DebugThingPlaceHelper.DebugSpawn(ChosenAmmo, parent.PositionHeld, Count);
                    count = 0;
                }
                chosenAmmo = value;
            }
        }
        public CompProps_Kit Props => (CompProps_Kit)props;
        #endregion Properties
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref count, "Count");
            Scribe_Values.Look(ref maxCount, "MaxCount");
            Scribe_Defs.Look(ref chosenAmmo, "ChosenAmmo");
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (this.Wearer.IsColonistPlayerControlled)
                yield return new Gizmo_Ammunition(this);
            yield break;
        }
    }

    public class CompProps_Kit : CompProperties
    {
        public int ammoCapacity;
        public AmmoTypes kitCategory;
        public CompProps_Kit()
        {
            compClass = typeof(KitComponent);
        }
    }
}
