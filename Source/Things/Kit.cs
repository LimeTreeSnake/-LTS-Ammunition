using Ammunition.Components;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Ammunition.Things
{
    [StaticConstructorOnStartup]
    public class Kit : Apparel
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            if (selPawn.Faction.IsPlayer && selPawn.RaceProps.intelligence > Intelligence.Animal)
            {
                KitComponent kit = this.TryGetComp<KitComponent>();
                if (kit != null && kit.Count > 0)
                {
                    void fetchAmmo()
                    {
                        Job job = JobMaker.MakeJob(Defs.JobDefOf.LTS_UnloadKit, this, selPawn);
                        job.playerForced = true;
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);                        
                    };
                    FloatMenuOption opt = new FloatMenuOption(Language.Translate.UnloadKit, fetchAmmo);
                    yield return opt;
                }
            }
            yield break;
        }
    }
}

