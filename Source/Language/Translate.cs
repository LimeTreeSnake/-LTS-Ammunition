using Verse;

namespace Ammunition.Language {
    [StaticConstructorOnStartup]
    public static class Translate {
        //LTSSystem
        public static string ChangePage => "LTSChangePage".Translate();
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
        public static string UnloadKit => "LTSUnloadKit".Translate();

        // Settings
        public static string Ammunition => "LTSAmmunition".Translate();
        public static string AmmunitionCategory => "LTSAmmunitionCategory".Translate();
        public static string AnimalAmmo => "LTSAnimalAmmo".Translate();
        public static string AnimalAmmoDesc => "LTSAnimalAmmoDesc".Translate();
        public static string AvailableAmmoCategory => "LTSAvailableAmmoCategory".Translate();
        public static string AvailableKits => "LTSAvailableKits".Translate();
        public static string BagNumber(int x) => "LTSBagNumber".Translate(x);
        public static string AddBag=> "LTSAddBag".Translate();
        public static string ChangeBagDesc => "LTSChangeBagDesc".Translate();
        public static string DefaultAssociation => "LTSDefaultAssociation".Translate();
        public static string Disable => "LTSDisable".Translate();
        public static string DisableMod => "LTSDisableMod".Translate();
        public static string Enable => "LTSEnable".Translate();
        public static string EnableMod => "LTSEnableMod".Translate();
        public static string ExemptWeapon => "LTSExemptWeapon".Translate();
        public static string HideMultipleGizmos => "LTSHideMultipleGizmos".Translate();
        public static string HideMultipleGizmosDesc => "LTSHideMultipleGizmosDesc".Translate();
        public static string MechanoidAmmo => "LTSMechanoidAmmo".Translate();
        public static string MechanoidAmmoDesc => "LTSMechanoidAmmoDesc".Translate();
        public static string LtsUseWiderGizmo => "LTSUseWiderGizmo".Translate();
        public static string LtsUseWiderGizmoDesc => "LTSUseWiderGizmoDesc".Translate();
        public static string LtsUseSingleLineAmmo => "LTSUseSingleLineAmmo".Translate();
        public static string LtsUseSingleLineAmmoDesc => "LTSUseSingleLineAmmoDesc".Translate();
        public static string NoAmmoCategories => "LTSNoAmmoCategories".Translate();
        public static string NpcMaxAmmo(int percentage) => "LTSNpcMaxAmmo".Translate(percentage);
        public static string NpcMaxAmmoDesc => "LTSNpcMaxAmmoDesc".Translate();
        public static string NpcMinAmmo(int percentage) => "LTSNpcMinAmmo".Translate(percentage);
        public static string NpcMinAmmoDesc => "LTSNpcMinAmmoDesc".Translate();
        public static string NpcUseAmmo => "LTSNpcUseAmmo".Translate();
        public static string NpcUseAmmoDesc => "LTSNpcUseAmmoDesc".Translate();
        public static string PerBullet => "LTSPerBullet".Translate();
        public static string PerBulletDesc => "LTSPerBulletDesc".Translate();
        public static string SelectedStandardAmmo => "LTSSelectedStandardAmmo".Translate();
        public static string UseAmmo(string cat) => "LTSUseAmmo".Translate(cat);
        public static string Weapons => "LTSWeapons".Translate();
    }
}
