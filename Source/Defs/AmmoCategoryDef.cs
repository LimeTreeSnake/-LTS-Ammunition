using Ammunition.Logic;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Ammunition.Defs {
    public class AmmoCategoryDef : Def {
        public List<string> ammoDefs;
        public List<string> includeWeaponDefs;
        public List<string> excludeWeaponDefs;
        public List<int> weightList;
        public AmmoTypes ammoType;
        public bool autoAssignable = true;
        public int ammoWeight = 1;
    }

    [DefOf]
    public static class AmmoCategoryDefOf {

        static AmmoCategoryDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(AmmoCategoryDefOf));
        }
    }
}
