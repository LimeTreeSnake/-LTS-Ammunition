using Verse;

namespace Ammunition.Language {
    [StaticConstructorOnStartup]
    public static class Translate {
        //LTSSystem
        public static string DefaultSettings => "LTSDefaultSettings".Translate();
        public static string Filter => "LTSFilter".Translate();

        // Messages
        public static string DesignatorLootAmmo => "LTSDesignatorLootAmmo".Translate();
        public static string DesignatorLootAmmoDesc => "LTSDesignatorLootAmmoDesc".Translate();
        public static string DesignatorCannotLootAmmo => "LTSDesignatorCannotLootAmmo".Translate();
        public static string MessageMustDesignateLootable => "LTSMessageMustDesignateLootable".Translate();
        public static string NoAmmo => "LTSNoAmmo".Translate();
        public static string DropAll => "LTSDropAll".Translate();
        public static string CanUse => "LTSCanUse".Translate();
        public static string NoAmmoDesc => "LTSNoAmmoDesc".Translate();
        public static string OutOfAmmo(Pawn pawn, string ammo) => "LTSOutOfAmmo".Translate(ammo, pawn.Named("PAWN"));

        // Settings
        public static string Ammunition => "LTSAmmunition".Translate();
        public static string AvailableAmmoCategory => "LTSAvailableAmmoCategory".Translate();
        public static string AvailableAmmoCategoryDesc => "LTSAvailableAmmoCategoryDesc".Translate();
        public static string BagNumber(int x) => "LTSBagNumber".Translate(x);
        public static string AddSlot=> "LTSAddSlot".Translate();
        public static string ChangeBag => "LTSChangeBag".Translate();
        public static string ConfigureKit => "LTSConfigureKit".Translate();
        public static string AmmoFilterOptions => "LTSAmmoFilterOptions".Translate();
        public static string AmmoWeaponsOptions => "LTSAmmoWeaponsOptions".Translate();
        public static string AmmoCategoryOptions(string cat) => "LTSAmmoCategoryOptions".Translate(cat);
        public static string AmmoUsageOptions => "LTSAmmoUsageOptions".Translate();
        public static string AmmoSearchOptions => "LTSAmmoSearchOptions".Translate();
        public static string HideMultipleGizmos => "LTSHideMultipleGizmos".Translate();
        public static string HideMultipleGizmosDesc => "LTSHideMultipleGizmosDesc".Translate();
        public static string LtsUseWiderGizmo => "LTSUseWiderGizmo".Translate();
        public static string LtsUseWiderGizmoDesc => "LTSUseWiderGizmoDesc".Translate();
        public static string NoAmmoCategories => "LTSNoAmmoCategories".Translate();
        public static string NpcMaxAmmo(int min, int max) => "LTSNpcMaxAmmo".Translate(min, max);
        public static string NpcUseAmmo => "LTSNpcUseAmmo".Translate();
        public static string NpcUseAmmoDesc => "LTSNpcUseAmmoDesc".Translate();
        public static string PerBullet => "LTSPerBullet".Translate();
        public static string PerBulletDesc => "LTSPerBulletDesc".Translate();
        public static string SettingsGeneral => "LTSSettingsGeneral".Translate();
        public static string SettingsKitsConfiguration => "LTSSettingsKitsConfiguration".Translate();
        public static string SettingsAmmoConfiguration => "LTSSettingsAmmoConfiguration".Translate();
    }
}
