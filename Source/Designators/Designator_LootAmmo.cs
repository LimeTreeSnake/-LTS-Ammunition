using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;


namespace Ammunition.Designators {
    public class Designator_LootAmmo : Designator {

        public override int DraggableDimensions => 2;
        protected override DesignationDef Designation => Defs.DesignationDefOf.LTS_LootAmmo;

        public Designator_LootAmmo() {
            defaultLabel = Language.Translate.DesignatorLootAmmo;
            defaultDesc = Language.Translate.DesignatorLootAmmoDesc;
            icon = ContentFinder<Texture2D>.Get("Icons/AmmoIcon", true);
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Claim;
        }
        public override AcceptanceReport CanDesignateThing(Thing t) {
            if (Map.designationManager.DesignationOn(t, Designation) != null) {
                return false;
            }
            return Logic.AmmoLogic.CanBeLootedByColony(t);
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c) {
            if (!c.InBounds(Map)) {
                return false;
            }
            if (!LootablesInCell(c).Any<Thing>()) {
                return Language.Translate.MessageMustDesignateLootable;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 c) {
            foreach (Thing item in LootablesInCell(c)) {
                DesignateThing(item);
            }
        }
        public override void DesignateThing(Thing t) {
            Map.designationManager.AddDesignation(new Designation(t, Designation));
            StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(t);
        }
        private IEnumerable<Thing> LootablesInCell(IntVec3 c) {
            if (c.Fogged(Map)) {
                yield break;
            }

            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++) {
                if (CanDesignateThing(thingList[i]).Accepted) {
                    yield return thingList[i];
                }
            }
        }

    }
}
