using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Ammunition.Toils {
    public static class Toils_Take {
        public static Toil LoadMagazine(TargetIndex ind, Ammunition.Components.KitComponent kit) {
            try {
                Toil toil = new Toil();
                toil.initAction = delegate {
                    Pawn actor = toil.actor;
                    Thing thing = actor.CurJob.GetTarget(ind).Thing;
                    int amount = Mathf.Min(thing.stackCount, kit.MaxCount - kit.Count);
                    kit.Count += amount;
                    thing.SplitOff(amount);
                    thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                };
                return toil;
            } catch (Exception ex) {
                Log.Message("LoadMagazine toil Try/Catch error! - " + ex.Message);
                return null;
            }
        }

        public static Toil OpportunisticLoadMagazine(Toil fetch, TargetIndex fetchInd, ThingDef def, Components.KitComponent kit) {
            try {
                Toil toil = new Toil();
                toil.initAction = delegate {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Predicate<Thing> validator = delegate (Thing t) {
                        if (!t.Spawned) {
                            return false;
                        }
                        if (t.IsForbidden(actor)) {
                            return false;
                        }
                        if (!actor.CanReserve(t)) {
                            return false;
                        }
                        if (kit.Count >= kit.MaxCount) {
                            return false;
                        }
                        return true;
                    };
                    Thing ammo = GenClosest.ClosestThing_Global_Reachable(actor.Position, actor.Map, actor.Map.listerThings.ThingsOfDef(def), PathEndMode.OnCell, TraverseParms.For(actor), 10, validator);
                    if (ammo != null) {
                        curJob.SetTarget(fetchInd, ammo);
                        actor.jobs.curDriver.JumpToToil(fetch);
                    }
                };
                return toil;
            } catch (Exception ex) {
                Log.Message("OpportunisticLoadMagazine toil Try/Catch error! - " + ex.Message);
                return null;
            }
        }

        public static Toil UnloadKit(TargetIndex ind, Ammunition.Components.KitComponent kit)
        {
            try
            {
                Toil toil = new Toil();
                toil.initAction = delegate {
                    Pawn actor = toil.actor;
                    Thing thing = actor.CurJob.GetTarget(ind).Thing;
                    DebugThingPlaceHelper.DebugSpawn(kit.ChosenAmmo, kit.parent.PositionHeld, kit.Count);
                    kit.Count = 0;
                    thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                };
                return toil;
            }
            catch (Exception ex)
            {
                Log.Message("UnloadKit toil Try/Catch error! - " + ex.Message);
                return null;
            }
        }

        //public static Toil TakeToInventory(TargetIndex ind, Func<int> countGetter) {
        //    Toil takeThing = new Toil();
        //    takeThing.initAction = delegate {
        //        Pawn actor = takeThing.actor;
        //        Thing thing = actor.CurJob.GetTarget(ind).Thing;
        //        if (!thing.Spawned) {
        //            Log.Message(string.Concat(actor, " tried to take ", thing, " which isn't spawned."));
        //            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
        //            return;
        //        }
        //        if (thing.stackCount == 0) {
        //            Log.Message(string.Concat(actor, " tried to take ", thing, " which had stackcount 0."));
        //            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
        //            return;
        //        }
        //        int num = Mathf.Min(countGetter(), thing.stackCount);
        //        num = Math.Min(num, MassUtility.CountToPickUpUntilOverEncumbered(actor, thing));
        //        if (num < 1) {
        //            Messages.Message(Language.Translate.AmmoTooHeavy(actor), MessageTypeDefOf.NegativeEvent);
        //            return;
        //        }
        //        actor.inventory.innerContainer.TryAdd(thing.SplitOff(num));
        //        thing.def.soundPickup.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
        //    };
        //    return takeThing;
        //}
    }
}
