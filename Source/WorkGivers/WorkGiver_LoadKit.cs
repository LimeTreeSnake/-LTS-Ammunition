using Ammunition.Components;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;


namespace Ammunition.WorkGivers {
    public class WorkGiver_LoadKit : WorkGiver_Scanner {

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
            IEnumerable<KitComponent> list = Logic.AmmoLogic.GetWornKitComps(pawn).Where(x => x.Count != x.MaxCount);
            //Log.Message(list.Count() + " available kits");
            IEnumerable<Thing> things = null;
            foreach(KitComponent kit in list) {
                if(things == null) {
                    things = pawn.Map.listerThings.ThingsOfDef(kit.ChosenAmmo);
                }
                else {
                    things.Union(pawn.Map.listerThings.ThingsOfDef(kit.ChosenAmmo));
                }
            }
            return things;
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false) {
            return !(pawn.apparel.AnyApparel && Logic.AmmoLogic.GetWornKitComps(pawn).Where(x => x.Count != x.MaxCount).Count() > 0);
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            return !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, forced);
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            Job job = JobMaker.MakeJob(Defs.JobDefOf.LTS_FetchAmmo, t, Logic.AmmoLogic.GetWornKits(pawn).FirstOrDefault(x => x.GetComp<KitComponent>().ChosenAmmo.defName == t.def.defName && x.GetComp<KitComponent>().Count < x.GetComp<KitComponent>().MaxCount));
            job.playerForced = forced;
            return job;
        }
    }
}
