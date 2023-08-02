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
			this.FailOn(() => !Logic.AmmoLogic.CanBeLootedByColony(this.TargetThingA));
			var toil = new Toil
			{
				initAction = delegate
				{
					this.pawn.pather.StartPath(this.TargetThingA, PathEndMode.ClosestTouch);
				},
				defaultCompleteMode = ToilCompleteMode.PatherArrival
			};

			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return toil;
			yield return Toils_General.Wait(100).WithProgressBarToilDelay(TargetIndex.A);

			var toil2 = new Toil
			{
				initAction = delegate
				{
					Thing thing = this.job.targetA.Thing;
					this.Map.designationManager.DesignationOn(thing, Defs.DesignationDefOf.LTS_LootAmmo)?.Delete();
					Logic.AmmoLogic.LootAmmo(thing);
					this.pawn.records.Increment(Defs.RecordDefOf.LTS_BodiesLooted);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};

			yield return toil2;
		}

		public override object[] TaleParameters()
		{
			var corpse = this.TargetA.Thing as Corpse;
			return new object[2]
			{
				this.pawn,
				(corpse != null) ? corpse.InnerPawn : this.TargetA.Thing
			};
		}
	}

}