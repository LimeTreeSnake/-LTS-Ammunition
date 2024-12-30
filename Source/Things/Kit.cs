using Ammunition.Components;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Ammunition.Gizmos;
using Ammunition.Logic;

namespace Ammunition.Things {
    [StaticConstructorOnStartup]
    public class Kit : Apparel {
        private KitComponent kitComp;
        public void Initiate() {
            if (kitComp == null) {
                kitComp = (KitComponent)this.AllComps.FirstOrDefault(x => x is KitComponent);
            }
        }

        public override IEnumerable<Gizmo> GetWornGizmos() {
            foreach (var item in base.GetWornGizmos()) {
                yield return item;
            };
            if (Find.Selector.SelectedPawns.Count > 1 && Settings.Settings.HideMultipleGizmos) {
                yield break;
            }
            if (Settings.Settings.AvailableAmmo.EnumerableNullOrEmpty()) {
                yield break;
            }
            Initiate();
            if (this.Wearer.IsColonistPlayerControlled) {
                yield return new GizmoAmmunition(kitComp);
            }
        }

        
        public bool AnythingToStrip()
        {
	        return Enumerable.Any(KitComp.Bags, item => item.Count > 0);
        }

        public void Strip()
        {
	        AmmoLogic.EmptyKitAt(KitComp, this.SpawnedParentOrMe.PositionHeld);
        }

        public KitComponent KitComp {
            get {
                Initiate();
                return kitComp;
            }
        }
    }
}


