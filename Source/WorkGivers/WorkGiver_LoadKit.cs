using Ammunition.Components;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ammunition.Logic;
using Ammunition.Models;
using UnityEngine;
using Verse;
using Verse.AI;


namespace Ammunition.WorkGivers
{
	public class WorkGiver_LoadKit : WorkGiver_Scanner
	{

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			IEnumerable<Things.Kit> list = Logic.AmmoLogic.GetWornKits(pawn);
			if (!list.Any())
			{
				return null;
			}

			IEnumerable<Thing> things = new List<Thing>();
			return list.Aggregate(things, (current1, kit)
				=> kit.KitComp.Bags.Where(t => t.ChosenAmmo != null && t.Count < t.MaxCount)
					.Aggregate(current1, (current, t)
						=> current.Union(pawn.Map.listerThings.ThingsOfDef(t.ChosenAmmo))));
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return !(pawn.apparel.AnyApparel &&
			         AmmoLogic.GetWornKits(pawn).Any(x => x.KitComp.Bags.Any(y => y.Count < y.MaxCount)));
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return !t.IsForbidden(pawn) &&
			       pawn.CanReserve(t, 1, -1, null, forced) &&
			       AmmoLogic.AvailableAmmo.Contains(t.def);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Job job = JobMaker.MakeJob(Defs.JobDefOf.LTS_FetchAmmo, t,
				AmmoLogic.GetWornKits(pawn)?.FirstOrDefault(x =>
						x.KitComp.Bags.Any(y => y.ChosenAmmo.defName == t.def.defName && y.Count < y.MaxCount)));

			job.playerForced = forced;
			return job;
		}
	}
}