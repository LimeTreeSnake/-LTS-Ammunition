using Ammunition.Defs;
using Ammunition.Language;
using Ammunition.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace Ammunition.Settings {
    public class AmmunitionSettings : Mod {

        private Settings settings;

        public AmmunitionSettings(ModContentPack content) : base(content) {
            settings = GetSettings<Settings>();
            settings.Initialize();
        }

        public override string SettingsCategory() {
            return "Ammunition";
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            settings.DoWindowContents(inRect);
        }
    }


    public class Settings : ModSettings {

        public Texture2D ammo_Disabled => ContentFinder<Texture2D>.Get("Icons/Ammo_disabled", true);
        public Texture2D ammo_Enabled => ContentFinder<Texture2D>.Get("Icons/Ammo_enabled", true);
        #region Fields
        private static bool useAmmoPerBullet = false;
        private static bool useMechanoidAmmo = true;
        private static bool useAnimalAmmo = true;
        private static bool hideMultipleGizmos = true;
        private static AmmoCategoryDef chosenCategory = null;
        private static bool enable, disable, enableMod, disableMod, changesMade;
        private static bool
        Neolithic = true,
        Medieval = true,
        Industrial = true,
        Spacer = true,
        Ultra = true,
        Archotech = true;
        private static string SearchFilter = "";
        private Vector2 scrollPos;
        #endregion Fields


        public static bool UseAmmoPerBullet => useAmmoPerBullet;
        public static bool UseMechanoidAmmo => useMechanoidAmmo;
        public static bool UseAnimalAmmo => useAnimalAmmo;
        public static bool HideMultipleGizmos => hideMultipleGizmos;
        public static Dictionary<string, Dictionary<string, bool>> CategoryWeaponDictionary;
        public static Dictionary<string, bool> ExemptionWeaponDictionary;

        public override void ExposeData() {
            Scribe_Values.Look(ref useAmmoPerBullet, "UseAmmoPerBullet", false, false);
            Scribe_Values.Look(ref useMechanoidAmmo, "UseMechanoidAmmo", false, false);
            Scribe_Values.Look(ref useAnimalAmmo, "UseAnimalAmmo", false, false);
            Scribe_Values.Look(ref hideMultipleGizmos, "HideMultipleGizmos", true, false);
            base.ExposeData();
        }

        public void Initialize() {
            if (CategoryWeaponDictionary == null) {
                CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
            }
            if (ExemptionWeaponDictionary == null) {
                ExemptionWeaponDictionary = new Dictionary<string, bool>();
            }
        }


        public void DoWindowContents(Rect inRect) {
            try {
                inRect.yMin += 20f;
                inRect.yMax -= 20f;
                float lineHeight = Text.LineHeight;
                Rect PageRect = new Rect(inRect.x, inRect.y, inRect.width - 30f, inRect.height - 30f);
                Listing_Standard list = new Listing_Standard();
                list.Begin(PageRect);
                Rect rect1 = list.GetRect(lineHeight);
                Widgets.CheckboxLabeled(rect1.LeftHalf(), Translate.PerBullet, ref useAmmoPerBullet);
                    if (Mouse.IsOver(rect1.LeftHalf())) {
                        Widgets.DrawHighlight(rect1.LeftHalf());
                    }
                TooltipHandler.TipRegion(rect1.LeftHalf(), Translate.PerBulletDesc);
                Widgets.CheckboxLabeled(rect1.RightHalf(), Translate.HideMultipleGizmos, ref hideMultipleGizmos);
                if (Mouse.IsOver(rect1.RightHalf())) {
                    Widgets.DrawHighlight(rect1.RightHalf());
                }
                TooltipHandler.TipRegion(rect1.RightHalf(), Translate.HideMultipleGizmosDesc);

                Rect rect2 = list.GetRect(lineHeight);
                Widgets.CheckboxLabeled(rect2.LeftHalf(), Translate.MechanoidAmmo, ref useMechanoidAmmo);
                if (Mouse.IsOver(rect2.LeftHalf())) {
                    Widgets.DrawHighlight(rect2.LeftHalf());
                }
                TooltipHandler.TipRegion(rect2.LeftHalf(), Translate.MechanoidAmmoDesc);

                Rect rect3 = list.GetRect(lineHeight);
                Widgets.CheckboxLabeled(rect3.LeftHalf(), Translate.AnimalAmmo, ref useAnimalAmmo);
                if (Mouse.IsOver(rect3.LeftHalf())) {
                    Widgets.DrawHighlight(rect3.LeftHalf());
                }
                TooltipHandler.TipRegion(rect3.LeftHalf(), Translate.AnimalAmmoDesc);


                list.GapLine(12f);
                if (AmmoLogic.AmmoCategoryDefs.Count() > 0) {
                    list.Label(Translate.AmmoCategorySelection, -1f, null);
                    if (chosenCategory == null) {
                        chosenCategory = AmmoLogic.AmmoCategoryDefs.First();
                    }
                    Rect innerRect = list.GetRect(lineHeight);
                    innerRect.width = PageRect.width / 4;
                    Widgets.Dropdown(innerRect, chosenCategory, (AmmoCategoryDef selectedThing) => _ = selectedThing, GenerateAmmoCategoryMenu, chosenCategory?.label);
                    list.Label(Translate.AvailableAmmoCategory, -1f, null);
                    Rect AmmoRect = list.GetRect(lineHeight * 2f);
                    Rect TextRect = list.GetRect(lineHeight * 1f);
                    for (int i = 0; i < chosenCategory.ammoDefs.Count; i++) {
                        ThingDef tempDef = DefDatabase<ThingDef>.GetNamed(chosenCategory.ammoDefs[i]);
                        Rect tempRect = new Rect((AmmoRect.x + (float)(i * lineHeight * 2f)), AmmoRect.y, lineHeight * 2, lineHeight * 2);
                        Widgets.DrawTextureFitted(tempRect, tempDef.graphic.MatSingle.mainTexture, 1);
                        if (Mouse.IsOver(tempRect)) {
                            float tempY = TextRect.y;
                            Widgets.DrawHighlight(tempRect);
                            Widgets.Label(TextRect.x + (float)(i * lineHeight), ref tempY, lineHeight * 10, tempDef.label);
                        }
                    }
                    list.GapLine(12f);
                    Rect settings = list.GetRect(lineHeight);
                    Widgets.Label(settings, Translate.AmmoCategoryOptions);
                    if (Widgets.ButtonText(settings.LeftHalf().RightHalf(), Translate.DefaultAssociation)) {
                        AmmoLogic.ResetInitialize();
                    }
                    if (Widgets.ButtonText(settings.RightHalf().LeftHalf(), Translate.Enable)) {
                        enable = true;
                    }
                    if (Widgets.ButtonText(settings.RightHalf().RightHalf(), Translate.Disable)) {
                        disable = true;
                    }
                    Rect settings2 = list.GetRect(lineHeight);
                    if (Widgets.ButtonText(settings2.RightHalf().LeftHalf(), Translate.EnableMod)) {
                        enableMod = true;
                    }
                    if (Widgets.ButtonText(settings2.RightHalf().RightHalf(), Translate.DisableMod)) {
                        disableMod = true;
                    }
                    list.GapLine(12f);
                    Rect optionsRect = list.GetRect(lineHeight * 10f);
                    Widgets.DrawMenuSection(optionsRect);
                    float margin = 4f;
                    float optionsHeight = optionsRect.height - (8f);
                    float yPos = optionsRect.y + margin;
                    float xPos = optionsRect.x + margin;
                    Rect filterRect = optionsRect.LeftPartPixels(150);
                    Rect WeaponsInRect = optionsRect.RightPartPixels(optionsRect.width - 150);
                    TextAnchor tempAnchor = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(filterRect.TopHalf().TopHalf().TopHalf().ContractedBy(4), Translate.Filter);
                    Text.Anchor = tempAnchor;
                    SearchFilter = Widgets.TextField(filterRect.TopHalf().TopHalf().BottomHalf().ContractedBy(2), SearchFilter);
                    Widgets.CheckboxLabeled(filterRect.TopHalf().BottomHalf().TopHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)2), ref Neolithic);
                    Widgets.CheckboxLabeled(filterRect.TopHalf().BottomHalf().BottomHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)3), ref Medieval);
                    Widgets.CheckboxLabeled(filterRect.BottomHalf().TopHalf().TopHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)4), ref Industrial);
                    Widgets.CheckboxLabeled(filterRect.BottomHalf().TopHalf().BottomHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)5), ref Spacer);
                    Widgets.CheckboxLabeled(filterRect.BottomHalf().BottomHalf().TopHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)6), ref Ultra);
                    Widgets.CheckboxLabeled(filterRect.BottomHalf().BottomHalf().BottomHalf().ContractedBy(2), TechLevelUtility.ToStringHuman((TechLevel)7), ref Archotech);
                    Widgets.DrawMenuSection(WeaponsInRect);
                    Rect WeaponsOutRect = new Rect(WeaponsInRect);
                    float num2 = (AmmoLogic.AvailableProjectileWeapons.Where(x =>
                    (x.techLevel == TechLevel.Neolithic && Neolithic) ||
                    (x.techLevel == TechLevel.Medieval && Medieval) ||
                    (x.techLevel == TechLevel.Industrial && Industrial) ||
                    (x.techLevel == TechLevel.Spacer && Spacer) ||
                    (x.techLevel == TechLevel.Ultra && Ultra) ||
                    (x.techLevel == TechLevel.Archotech && Archotech)
                    ).Count() * lineHeight) / 3;
                    if (num2 < WeaponsOutRect.height) {
                        num2 = WeaponsOutRect.height;
                    }
                    Rect WeaponsViewRect = new Rect(0f, 0f, WeaponsOutRect.width - 16f, num2);
                    Widgets.BeginScrollView(WeaponsOutRect, ref this.scrollPos, WeaponsViewRect, true);
                    Listing_Standard WeaponsList = new Listing_Standard(WeaponsInRect, () => this.scrollPos) {
                        ColumnWidth = ((WeaponsInRect.width - 16) / 3) - (margin * 3)
                    };
                    WeaponsList.Begin(WeaponsViewRect);
                    foreach (ThingDef thingDef in AmmoLogic.AvailableProjectileWeapons) {
                        if (thingDef.label.ToLower().Contains(SearchFilter.ToLower())) {
                            switch (thingDef.techLevel) {
                                case TechLevel.Neolithic:
                                    if (!Neolithic) {
                                        continue;
                                    }
                                    break;
                                case TechLevel.Medieval:
                                    if (!Medieval) {
                                        continue;
                                    }
                                    break;
                                case TechLevel.Industrial:
                                    if (!Industrial) {
                                        continue;
                                    }
                                    break;
                                case TechLevel.Spacer:
                                    if (!Spacer) {
                                        continue;
                                    }
                                    break;
                                case TechLevel.Ultra:
                                    if (!Ultra) {
                                        continue;
                                    }
                                    break;
                                case TechLevel.Archotech:
                                    if (!Archotech) {
                                        continue;
                                    }
                                    break;
                                default:
                                    break;
                            }
                            Rect weaponRect = WeaponsList.GetRect(lineHeight);
                            Rect mainWeaponRect = weaponRect.LeftPartPixels(weaponRect.width - 25);

                            Rect subWeaponRect = weaponRect.RightPartPixels(25f);
                            float curY = weaponRect.y;
                            CategoryWeaponDictionary.TryGetValue(chosenCategory.defName, out Dictionary<string, bool> dic);
                            dic.TryGetValue(thingDef.defName, out bool res);
                            ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out bool exemption);
                            bool val = res;
                            bool val2 = exemption;
                            if (enable) {
                                dic.SetOrAdd(thingDef.defName, true);
                                changesMade = true;
                            }
                            if (disable) {
                                dic.SetOrAdd(thingDef.defName, false);
                                changesMade = true;
                            }
                            if (enableMod) {
                                ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
                                changesMade = true;
                            }
                            if (disableMod) {
                                ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, true);
                                changesMade = true;
                            }
                            Widgets.DrawHighlightIfMouseover(weaponRect);
                            TooltipHandler.TipRegion(mainWeaponRect, Translate.UseAmmo(chosenCategory.label));
                            Widgets.CheckboxLabeled(mainWeaponRect, " " + thingDef.LabelCap, ref val, false);
                            if (val != res) {
                                dic.SetOrAdd(thingDef.defName, val);
                                changesMade = true;
                            }
                            TooltipHandler.TipRegion(subWeaponRect, Translate.ExemptWeapon);
                            Widgets.Checkbox(subWeaponRect.x, subWeaponRect.y, ref val2, 24, false, false, ammo_Disabled, ammo_Enabled);
                            if (val2 != exemption) {
                                Log.Message(val2 + " exemption");
                                ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, val2);
                                changesMade = true;
                            }
                        }
                    }
                    WeaponsList.End();
                    Widgets.EndScrollView();
                    disable = false;
                    enable = false;
                    disableMod = false;
                    enableMod = false;
                    if (changesMade) {
                        Logic.AmmoLogic.Save();
                        changesMade = false;
                    }
                }
                else {
                    list.Label(Translate.NoAmmoCategories);
                }
                list.End();
                Write();
            }
            catch (Exception ex) {
                Log.Message(ex.Message);
            }
        }
        public static IEnumerable<Widgets.DropdownMenuElement<AmmoCategoryDef>> GenerateAmmoCategoryMenu(AmmoCategoryDef def) {
            List<AmmoCategoryDef>.Enumerator enumerator = AmmoLogic.AmmoCategoryDefs.ToList().GetEnumerator();
            while (enumerator.MoveNext()) {
                AmmoCategoryDef ammo = enumerator.Current;
                yield return new Widgets.DropdownMenuElement<AmmoCategoryDef> {
                    option = new FloatMenuOption(ammo.label, delegate () { chosenCategory = ammo; }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = ammo,
                };
            }
            yield break;
        }
    }
}