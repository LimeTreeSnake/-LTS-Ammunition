using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Ammunition.JobDrivers
{
    public class JobDriver_FetchAmmo : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = base.pawn;
            LocalTargetInfo targetA = base.job.targetA;
            Job job = base.job;
            return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);
            yield return reserveTargetA;
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            Apparel apparel = TargetB.Thing as Apparel;
            // Log.Message(TargetA.Label + " " + TargetB.Label);
            if (apparel != null)
            {
                Components.KitComponent kit = apparel.GetComp<Components.KitComponent>();
                if (kit != null && TargetA.Thing.def.defName == kit.ChosenAmmo.defName)
                {
                    yield return Toils.Toils_Take.LoadMagazine(TargetIndex.A, kit);
                    if (kit.Count != kit.MaxCount)
                        yield return Toils.Toils_Take.OpportunisticLoadMagazine(reserveTargetA, TargetIndex.A, TargetA.Thing.def, kit);
                }
            }
            else
            {
                Log.Message("Something went wrong getting my Ammo! :(");
            }
            yield break;
        }
    }

}
