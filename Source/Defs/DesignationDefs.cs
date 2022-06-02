using RimWorld;
using Verse;

namespace Ammunition.Defs {
    [DefOf]
    public static class DesignationDefOf {

        public static DesignationDef LTS_LootAmmo;

        static DesignationDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf));
        }
    }
}
