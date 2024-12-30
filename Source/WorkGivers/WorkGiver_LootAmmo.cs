using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;


namespace Ammunition.WorkGivers {
    public class WorkGiver_LootAmmo : WorkGiver_Scanner {

        public override PathEndMode PathEndMode {
            get {
                return PathEndMode.ClosestTouch;
            }
        }
        public override Danger MaxPathDanger(Pawn pawn) {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
            foreach (var item in pawn.Map.designationManager.SpawnedDesignationsOfDef(Defs.DesignationDefOf.LTS_LootAmmo)) {
                if (!item.target.HasThing) {
                    Log.ErrorOnce("Loot ammo designation has no target.", 63126);
                }
                else {
                    yield return item.target.Thing;
                }
            }
        }
        
        public override bool ShouldSkip(Pawn pawn, bool forced = false) {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(Defs.DesignationDefOf.LTS_LootAmmo);
        }
        
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            if (t.Map.designationManager.DesignationOn(t, Defs.DesignationDefOf.LTS_LootAmmo) == null) {
                return false;
            }

            if (!pawn.CanReserve(t, 1, -1, null, forced)) {
                return false;
            }

            if (!Logic.AmmoLogic.CanBeLootedByColony(t)) {
                return false;
            }

            return true;
        }
        
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            return JobMaker.MakeJob(Defs.JobDefOf.LTS_LootAmmo, t);
        }
    }
}
