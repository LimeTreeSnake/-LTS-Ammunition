using RimWorld;
using Verse;

namespace Ammunition.Defs {
    [DefOf]
    public static class JobDefOf {

        public static JobDef LTS_FetchAmmo;
        public static JobDef LTS_UnloadKit;
        public static JobDef LTS_LootAmmo;

        static JobDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
    }
}
