using Ammunition.Logic;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Ammunition.Defs {
    public class AmmoCategoryDef : Def {
        public List<string> ammoDefs;
        public List<string> includeWeaponDefs;
        public List<string> excludeWeaponDefs;
        public AmmoTypes ammoType;
    }

    [DefOf]
    public static class AmmoCategoryDefOf {

        static AmmoCategoryDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(AmmoCategoryDefOf));
        }
    }
}
