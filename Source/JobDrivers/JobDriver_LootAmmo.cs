using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Ammunition.JobDrivers
{
	public class JobDriver_LootAmmo : JobDriver_Strip
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnAggroMentalState(TargetIndex.A);
			this.FailOn(() => !Logic.AmmoLogic.CanBeLootedByColony(base.TargetThingA));
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				pawn.pather.StartPath(base.TargetThingA, PathEndMode.ClosestTouch);
			};

			toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return toil;
			yield return Toils_General.Wait(100).WithProgressBarToilDelay(TargetIndex.A);

			Toil toil2 = new Toil();
			toil2.initAction = delegate
			{
				Thing thing = job.targetA.Thing;
				Map.designationManager.DesignationOn(thing, Defs.DesignationDefOf.LTS_LootAmmo)?.Delete();
				Logic.AmmoLogic.LootAmmo(thing);
				pawn.records.Increment(Defs.RecordDefOf.LTS_BodiesLooted);
			};

			toil2.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return toil2;
		}

		public override object[] TaleParameters()
		{
			Corpse corpse = base.TargetA.Thing as Corpse;
			return new object[2]
			{
				pawn,
				(corpse != null) ? corpse.InnerPawn : base.TargetA.Thing
			};
		}
	}

}