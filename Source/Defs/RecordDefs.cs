using RimWorld;
using Verse;

namespace Ammunition.Defs {
    [DefOf]
    public static class RecordDefOf {

        public static RecordDef LTS_BodiesLooted;

        static RecordDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(RecordDefOf));
        }
    }
}
