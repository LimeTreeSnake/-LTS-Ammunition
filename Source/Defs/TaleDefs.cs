using RimWorld;
using Verse;


namespace Ammunition.Defs {
    [DefOf]
    public static class TaleDefOf {
        public static TaleDef LTS_Looted;

        static TaleDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(TaleDefOf));
        }
    }
}
