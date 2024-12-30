using System.Collections.Generic;
using Ammunition.Things;
using Verse;
using Verse.AI;

namespace Ammunition.JobDrivers {
    public class JobDriver_UnloadKit : JobDriver {
        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            var pawn = this.pawn;
            var targetA = base.job.targetA;
            var job = this.job;
            return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils() {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            var reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
            yield return reserveTargetA;
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            if (this.TargetA.Thing is Kit kit)
            {
	            yield return Toils.Toils_Take.UnloadKit(TargetIndex.A, kit.KitComp);
            }
            else {
                Log.Message("Something went wrong getting my Ammo! :(");
            }
        }
    }
   
}
