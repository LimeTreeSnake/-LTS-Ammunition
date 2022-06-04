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
            IEnumerable<Thing> things = null;
            IEnumerable<Things.Kit> list = Logic.AmmoLogic.GetWornKits(pawn);
            if (list.Any()) {
                foreach (Things.Kit kit in list) {
                    for (int i = 0; i < kit.KitComp.Props.bags; i++) {
                        if (kit.KitComp.Bags[i].ChosenAmmo != null && kit.KitComp.Bags[i].Count < kit.KitComp.Bags[i].MaxCount) {
                            if (things == null) {
                                things = pawn.Map.listerThings.ThingsOfDef(kit.KitComp.Bags[i].ChosenAmmo);
                            }
                            else if (!things.Any()) {
                                things = pawn.Map.listerThings.ThingsOfDef(kit.KitComp.Bags[i].ChosenAmmo);
                            }
                            else {
                                things.Union(pawn.Map.listerThings.ThingsOfDef(kit.KitComp.Bags[i].ChosenAmmo));
                            }
                        }
                    }
                }
            }
            return things;
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false) {
            return !(pawn.apparel.AnyApparel && Logic.AmmoLogic.GetWornKits(pawn).Count() > 0 && Logic.AmmoLogic.GetWornKits(pawn).Where(x=> x.KitComp.Bags.Where(y=> y.Count < y.MaxCount).Count() > 0).Count() > 0);
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            return !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, forced) && Logic.AmmoLogic.AvailableAmmo.Contains(t.def);
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            Job job = JobMaker.MakeJob(Defs.JobDefOf.LTS_FetchAmmo, t, Logic.AmmoLogic.GetWornKits(pawn)?.FirstOrDefault(x => x.KitComp.Props.bags > 0 && x.KitComp.Bags.Where(y => y.ChosenAmmo.defName == t.def.defName && y.Count < y.MaxCount).Any()));
            job.playerForced = forced;
            return job;
        }
    }
}
