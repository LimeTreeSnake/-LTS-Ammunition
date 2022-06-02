using Ammunition.Components;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;


namespace Ammunition.Things {
    [StaticConstructorOnStartup]
    public class Ammo : ThingWithComps {
        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
        //    foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn)) {
        //        yield return floatMenuOption;
        //    }
        //    if (selPawn.Faction.IsPlayer && selPawn.RaceProps.intelligence > Intelligence.Animal) {

        //        foreach (Apparel apparel in selPawn.apparel.WornApparel) {
        //            KitComponent kit = apparel.TryGetComp<KitComponent>();
        //            if (kit != null && kit.ChosenAmmo == this.def) {
        //                void fetchAmmo() {
        //                    Logic.AmmoLogic.FetchAmmo(selPawn, this, apparel);
        //                }
        //                yield return new FloatMenuOption(Language.Translate.AmmoFetchFor(apparel.LabelCap), fetchAmmo, MenuOptionPriority.High);
        //            }
        //        }

             
        //    }
        //}
    }
}
