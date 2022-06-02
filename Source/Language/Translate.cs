using Verse;

namespace Ammunition.Language {
    [StaticConstructorOnStartup]
    public static class Translate {
        // Settings
        public static string Ammunition => "LTSAmmunition".Translate();
        public static string AmmoCategorySelection => "LTSAmmoCategorySelection".Translate();
        public static string AmmoCategoryOptions => "LTSAmmoCategoryOptions".Translate();
        public static string AnimalAmmo => "LTSAnimalAmmo".Translate();
        public static string AnimalAmmoDesc => "LTSAnimalAmmoDesc".Translate();
        public static string AvailableAmmoCategory => "LTSAvailableAmmoCategory".Translate();
        public static string NoAmmoCategories => "LTSNoAmmoCategories".Translate();
        public static string Weapons => "LTSWeapons".Translate();
        public static string Enable => "LTSEnable".Translate();
        public static string Disable => "LTSDisable".Translate();
        public static string EnableMod => "LTSEnableMod".Translate();
        public static string DisableMod => "LTSDisableMod".Translate();
        public static string Filter => "LTSFilter".Translate();
        public static string DefaultAssociation => "LTSDefaultAssociation".Translate();
        public static string ExemptWeapon => "LTSExemptWeapon".Translate();
        public static string UseAmmo(string cat) => "LTSUseAmmo".Translate(cat);
        public static string MechanoidAmmo => "LTSMechanoidAmmo".Translate();
        public static string MechanoidAmmoDesc => "LTSMechanoidAmmoDesc".Translate();
        public static string PerBullet => "LTSPerBullet".Translate();
        public static string PerBulletDesc => "LTSPerBulletDesc".Translate();
        public static string HideMultipleGizmos => "LTSHideMultipleGizmos".Translate();
        public static string HideMultipleGizmosDesc => "LTSHideMultipleGizmosDesc".Translate();

        // Messages
        public static string NoAmmo => "LTSNoAmmo".Translate();
        public static string NoAmmoDesc => "LTSNoAmmoDesc".Translate();
        public static string OutOfAmmo(Pawn pawn, string ammo) => "LTSOutOfAmmo".Translate(ammo, pawn.Named("PAWN"));
        public static string UnloadKit => "LTSUnloadKit".Translate();
        public static string DesignatorLootAmmo => "LTSDesignatorLootAmmo".Translate();
        public static string DesignatorLootAmmoDesc => "LTSDesignatorLootAmmoDesc".Translate();
        public static string DesignatorCannotLootAmmo => "LTSDesignatorCannotLootAmmo".Translate();
        public static string MessageMustDesignateLootable => "LTSMessageMustDesignateLootable".Translate();

    }
}
