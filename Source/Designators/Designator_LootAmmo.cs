using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace Ammunition.Designators {
    public class Designator_LootAmmo : Designator {

        public override int DraggableDimensions => 2;
        protected override DesignationDef Designation => Defs.DesignationDefOf.LTS_LootAmmo;

        public Designator_LootAmmo() {
	        this.defaultLabel = Language.Translate.DesignatorLootAmmo;
	        this.defaultDesc = Language.Translate.DesignatorLootAmmoDesc;
	        this.icon = ContentFinder<Texture2D>.Get("Icons/AmmoIcon", true);
	        this.soundDragSustain = SoundDefOf.Designate_DragStandard;
	        this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
	        this.useMouseIcon = true;
	        this.soundSucceeded = SoundDefOf.Designate_Claim;
        }
        public override AcceptanceReport CanDesignateThing(Thing t) {
            if (t == null || this.Map.designationManager.DesignationOn(t, Designation) != null) {
                return false;
            }
            return Logic.AmmoLogic.CanBeLootedByColony(t);
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c) {
            if (!c.InBounds(this.Map)) {
                return false;
            }
            if (!LootablesInCell(c).Any<Thing>()) {
                return Language.Translate.MessageMustDesignateLootable;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 c) {
            foreach (var item in LootablesInCell(c)) {
                DesignateThing(item);
            }
        }
        public override void DesignateThing(Thing t) {
	        this.Map.designationManager.AddDesignation(new Designation(t, Designation));
            StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(t);
        }
        private IEnumerable<Thing> LootablesInCell(IntVec3 c) {
            if (c.Fogged(this.Map)) {
                yield break;
            }

            var thingList = c.GetThingList(this.Map);
            foreach (var t in thingList.Where(t => CanDesignateThing(t).Accepted))
            {
	            yield return t;
            }
        }

    }
}
