using System;
using System.Linq;
using Ammunition.Logic;
using Ammunition.Models;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Ammunition.Toils
{
	public static class Toils_Take
	{
		public static Toil LoadMagazine(TargetIndex ind, Ammunition.Components.KitComponent kit)
		{
			try
			{
				var toil = new Toil();
				toil.initAction = delegate
				{
					Pawn actor = toil.actor;
					Thing thing = actor.CurJob.GetTarget(ind).Thing;
					foreach (AmmoSlot t in kit.Bags)
					{
						if (t.ChosenAmmo != thing.def)
						{
							continue;
						}

						int amount = Mathf.Min(thing.stackCount, t.MaxCount - t.Count);
						if (amount > 0)
						{
							thing.SplitOff(amount);
							t.Count += amount;
							thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
						}

						if (amount == thing.stackCount)
						{
							break;
						}
					}
				};

				return toil;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("LoadMagazine toil Try/Catch error! - " + ex.Message);
				}

				return null;
			}
		}

		public static Toil OpportunisticLoadMagazine(Toil fetch,
			TargetIndex fetchInd,
			ThingDef def,
			Components.KitComponent kit)
		{
			try
			{
				var toil = new Toil();
				toil.initAction = delegate
				{
					Pawn actor = toil.actor;
					Job curJob = actor.jobs.curJob;

					bool Validator(Thing t)
					{
						if (!t.Spawned)
						{
							return false;
						}

						if (t.IsForbidden(actor))
						{
							return false;
						}

						return actor.CanReserve(t) &&
						       Enumerable.Any(kit.Bags, t1 => def == t1.ChosenAmmo && t1.Count < t1.MaxCount);

					}

					Thing ammo = GenClosest.ClosestThing_Global_Reachable(actor.Position, actor.Map,
						actor.Map.listerThings.ThingsOfDef(def), PathEndMode.OnCell, TraverseParms.For(actor), 10,
						Validator);

					if (ammo == null)
					{
						return;
					}

					curJob.SetTarget(fetchInd, ammo);
					actor.jobs.curDriver.JumpToToil(fetch);
				};

				return toil;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("OpportunisticLoadMagazine toil Try/Catch error! - " + ex.Message);
				}

				return null;
			}
		}

		public static Toil UnloadKit(TargetIndex ind, Components.KitComponent kit)
		{
			try
			{
				var toil = new Toil();
				toil.initAction = delegate
				{
					Pawn actor = toil.actor;
					Thing thing = actor.CurJob.GetTarget(ind).Thing;

					AmmoLogic.EmptyKitAt(kit, thing.PositionHeld);
					thing.def.soundInteract?.PlayOneShot(new TargetInfo(actor.Position, actor.Map));
				};

				return toil;
			}
			catch (Exception ex)
			{
				if (Settings.Settings.DebugLogs)
				{
					Log.Error("UnloadKit toil Try/Catch error! - " + ex.Message);
				}

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