using System;
using Ammunition.Logic;
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
                    for (int i = 0; i < kit.Bags.Count; i++) {
                        if (kit.Bags[i].ChosenAmmo == thing.def) {
                            int amount = Mathf.Min(thing.stackCount, kit.Bags[i].MaxCount - kit.Bags[i].Count);
                            if (amount > 0) {
                                thing.SplitOff(amount);
                                kit.Bags[i].Count += amount;
                                thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                            }
                            if (amount == thing.stackCount)
                                break;
                        }
                    }
                };
                return toil;
            }
            catch (Exception ex) {
                Log.Error("LoadMagazine toil Try/Catch error! - " + ex.Message);
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
                        for (int i = 0; i < kit.Bags.Count; i++) {
                            if (def == kit.Bags[i].ChosenAmmo && kit.Bags[i].Count < kit.Bags[i].MaxCount) {
                                return true;
                            }
                        }
                        return false;
                    };
                    Thing ammo = GenClosest.ClosestThing_Global_Reachable(actor.Position, actor.Map, actor.Map.listerThings.ThingsOfDef(def), PathEndMode.OnCell, TraverseParms.For(actor), 10, validator);
                    if (ammo != null) {
                        curJob.SetTarget(fetchInd, ammo);
                        actor.jobs.curDriver.JumpToToil(fetch);
                    }
                };
                return toil;
            }
            catch (Exception ex) {
                Log.Error("OpportunisticLoadMagazine toil Try/Catch error! - " + ex.Message);
                return null;
            }
        }

        public static Toil UnloadKit(TargetIndex ind, Components.KitComponent kit) {
            try {
                Toil toil = new Toil();
                toil.initAction = delegate {
                    Pawn actor = toil.actor;
                    Thing thing = actor.CurJob.GetTarget(ind).Thing;
                    
                    AmmoLogic.EmptyKitAt(kit, thing.PositionHeld);
                    thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
                };
                return toil;
            }
            catch (Exception ex) {
                Log.Error("UnloadKit toil Try/Catch error! - " + ex.Message);
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
