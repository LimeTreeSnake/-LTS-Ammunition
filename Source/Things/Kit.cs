using Ammunition.Components;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Ammunition.Gizmos;
using System;

namespace Ammunition.Things {
    [StaticConstructorOnStartup]
    public class Kit : Apparel {
        private KitComponent kitComp;
        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
        //    foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn)) {
        //        yield return floatMenuOption;
        //    }
        //    Initiate();
        //    if (selPawn.Faction.IsPlayer && selPawn.RaceProps.intelligence > Intelligence.Animal) {
        //        if (kitComp != null && kitComp.Bags.Where(x => x.Count > 0).Any()) {
        //            void unloadKit() {
        //                Job job = JobMaker.MakeJob(Defs.JobDefOf.LTS_UnloadKit, this, selPawn);
        //                job.playerForced = true;
        //                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
        //            };
        //            FloatMenuOption opt = new FloatMenuOption(Language.Translate.UnloadKit, unloadKit);
        //            yield return opt;
        //        }
        //    }
        //    yield break;
        //}
        public void Initiate() {
            if (kitComp == null) {
                kitComp = (KitComponent)AllComps.FirstOrDefault(x => x is KitComponent);
            }
        }

        public override IEnumerable<Gizmo> GetWornGizmos() {
            foreach (var item in base.GetWornGizmos()) {
                yield return item;
            };
            if (Find.Selector.SelectedPawns.Count > 1 && Settings.Settings.HideMultipleGizmos) {
                yield break;
            }
            if (Logic.AmmoLogic.AvailableAmmo.EnumerableNullOrEmpty()) {
                yield break;
            }
            Initiate();
            if (this.Wearer.IsColonistPlayerControlled) {
                yield return new Gizmo_Ammunition(ref kitComp);
            }
            yield break;
        }

        public bool AnythingToStrip() {
            foreach (var item in KitComp.Bags) {
                if (item.Count > 0)
                    return true;
            }
            return false;
        }

        public void Strip() {
            IntVec3 pos = this.SpawnedParentOrMe.PositionHeld;
            foreach (var item in KitComp.Bags) {
                if (item.Count > 0) {
                    DebugThingPlaceHelper.DebugSpawn(item.ChosenAmmo, pos, item.Count);
                    item.Count = 0;
                }
            }
        }

        public KitComponent KitComp {
            get {
                Initiate();
                return kitComp;
            }
        }
    }
}


