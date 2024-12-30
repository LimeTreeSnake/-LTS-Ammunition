using System;
using System.Collections.Generic;
using System.Linq;
using Ammunition.Components;
using Ammunition.DefModExtensions;
using Ammunition.Defs;
using Ammunition.Language;
using Ammunition.Logic;
using LTS_Systems.Dialog;
using LTS_Systems.GUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ammunition.Settings
{
    public class AmmunitionSettings : Mod
    {
        private readonly Settings _settings;

        public AmmunitionSettings(ModContentPack content) : base(content)
        {
            _settings = this.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "LTS Ammunition";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            _settings.DoWindowContents(inRect);
        }
    }

    public class Settings : ModSettings
    {
        public static Texture2D FalseIcon => Icons.False;
        public static Texture2D TrueIcon => Icons.True;
        private static Texture2D PartialIcon => Icons.Partial;
        public static Texture2D DropIcon => Icons.Down;
        public static Texture2D InfoIcon => Icons.Info;

        private static bool _useAmmoPerBullet = true;
        private static bool _hideMultipleGizmos = true;
        private static bool _useCompactGizmo;
        private static bool _npcUseAmmo = true;
        private static bool _debugLogs;
        private static IntRange _range = new IntRange(30, 60);
        private static AmmoCategoryDef _chosenCategory;
        private static ThingDef _chosenKit;
        private static ModContentPack _chosenMod;
        private static bool _page1 = true, _page2, _page3;
        private static bool _changesMade, _minorChangesMade;
        private static bool _filterCatFalse, _filterCatTrue, _filterAmmoFalse, _filterAmmoTrue;
        private static bool _setAllAmmoRequiredTrue, _setAllAmmoRequiredFalse, _setAllCatTrue, _setAllCatFalse;

        private static bool
            _neolithic = true,
            _medieval = true,
            _industrial = true,
            _spacer = true,
            _ultra = true,
            _archotech = true,
            _unused = false;

        private static string _searchFilter = "";
        private Vector2 _scrollWeaponsPos;
        private const float Margin = 4f;
        private static readonly float _lineHeight = Text.LineHeight;
        public static bool HideMultipleGizmos => _hideMultipleGizmos;
        public static IntRange Range => _range;
        public static bool UseAmmoPerBullet => _useAmmoPerBullet;
        public static bool UseCompactGizmo => _useCompactGizmo;
        public static bool NpcUseAmmo => _npcUseAmmo;
        public static bool DebugLogs => _debugLogs;

        //Dictionaries
        private static Dictionary<string, bool> _exemptionWeaponDictionary;

        public static Dictionary<string, bool> ExemptionWeaponDictionary
        {
            get { return _exemptionWeaponDictionary ?? (_exemptionWeaponDictionary = new Dictionary<string, bool>()); }
        }

        private static Dictionary<string, Dictionary<string, bool>> _categoryWeaponDictionary;

        public static Dictionary<string, Dictionary<string, bool>> CategoryWeaponDictionary
        {
            get
            {
                return _categoryWeaponDictionary
                       ?? (_categoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>());
            }
        }

        private static Dictionary<string, BagSettings> _bagSettingsDictionary;

        public static Dictionary<string, BagSettings> BagSettingsDictionary
        {
            get { return _bagSettingsDictionary ?? (_bagSettingsDictionary = new Dictionary<string, BagSettings>()); }
        }

        private static Dictionary<ThingDef, List<AmmoCategoryDef>> _ammoToCategoriesDictionary;

        public static Dictionary<ThingDef, List<AmmoCategoryDef>> AmmoToCategoriesDictionary
        {
            get
            {
                return _ammoToCategoriesDictionary
                       ?? (_ammoToCategoriesDictionary = new Dictionary<ThingDef, List<AmmoCategoryDef>>());
            }
        }

        private static HashSet<ThingDef> _availableProjectileWeapons;

        public static HashSet<ThingDef> AvailableProjectileWeapons
        {
            get
            {
                return _availableProjectileWeapons
                       ?? (_availableProjectileWeapons = DefDatabase<ThingDef>
                           .AllDefsListForReading
                           .Where(AmmoLogic.IsViableWeaponDef)
                           .ToHashSet());
            }
        }

        private static HashSet<ThingDef> _availableAmmo;

        public static HashSet<ThingDef> AvailableAmmo
        {
            get
            {
                return _availableAmmo
                       ?? (_availableAmmo = DefDatabase<ThingDef>.AllDefsListForReading
                           .Where(x => x.HasModExtension<AmmunitionExtension>())
                           .ToHashSet());
            }
        }

        private static HashSet<ThingDef> _availableKits;

        public static HashSet<ThingDef> AvailableKits
        {
            get
            {
                return _availableKits
                       ?? (_availableKits = DefDatabase<ThingDef>.AllDefsListForReading
                           .Where(x => x.HasComp(typeof(KitComponent)))
                           .ToHashSet());
            }
        }

        private static HashSet<AmmoCategoryDef> _ammoCategoryDefs;

        public static HashSet<AmmoCategoryDef> AmmoCategoryDefs
        {
            get
            {
                return _ammoCategoryDefs
                       ?? (_ammoCategoryDefs = DefDatabase<AmmoCategoryDef>.AllDefsListForReading.ToHashSet());
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref _useAmmoPerBullet, "UseAmmoPerBullet", true);
            Scribe_Values.Look(ref _hideMultipleGizmos, "HideMultipleGizmos", true);
            Scribe_Values.Look(ref _useCompactGizmo, "UseCompactGizmo");
            Scribe_Values.Look(ref _npcUseAmmo, "NpcUseAmmo", true);
            Scribe_Values.Look(ref _debugLogs, "DebugLogs", false);
            Scribe_Values.Look(ref _range, "Range", new IntRange(30, 60));
            base.ExposeData();
        }

        private static void Reset()
        {
            _useAmmoPerBullet = true;
            _hideMultipleGizmos = true;
            _useCompactGizmo = false;
            _npcUseAmmo = true;
            _debugLogs = false;
            _range = new IntRange(30, 60);
            AmmoLogic.ResetInitialize();
        }

        public void DoWindowContents(Rect inRect)
        {
            try
            {
                var list = new Listing_Standard();
                list.Begin(inRect);

                //Top Settings
                var topSettingsRect = list.GetRect(_lineHeight * 2);
                topSettingsRect.SplitVertically(inRect.width / 1.5f,
                    out var topSettingsRectLeft,
                    out var topSettingsRectRight);

                topSettingsRectLeft.SplitVertically(topSettingsRectLeft.width / 3,
                    out var generalSettingsRect,
                    out var otherSettingsRectRight);

                otherSettingsRectRight.SplitVertically(otherSettingsRectRight.width / 2,
                    out var kitsSettingsRect,
                    out var ammoSettingsRect);

                if (Widgets.ButtonText(generalSettingsRect.ContractedBy(0, Margin),
                        LTS_Systems.Language.Translate.GeneralSettings))
                {
                    _page1 = true;
                    _page2 = false;
                    _page3 = false;
                }

                if (Widgets.ButtonText(kitsSettingsRect.ContractedBy(0, Margin), Translate.SettingsKitsConfiguration))
                {
                    _page1 = false;
                    _page2 = true;
                    _page3 = false;
                }

                if (Widgets.ButtonText(ammoSettingsRect.ContractedBy(0, Margin), Translate.SettingsAmmoConfiguration))
                {
                    _page1 = false;
                    _page2 = false;
                    _page3 = true;
                }

                if (Widgets.ButtonText(topSettingsRectRight.RightHalf().BottomHalf(),
                        LTS_Systems.Language.Translate.DefaultSettings))
                {
                    var dialogConfirm = new DialogConfirm(Reset);
                    Find.WindowStack.Add(dialogConfirm);
                    _changesMade = true;
                }

                list.GapLine(2);

                //General Settings
                if (_page1)
                {
                    DrawGeneralSettingsPage(list);
                }
                // Kit Configuration
                else if (_page2)
                {
                    DrawKitConfigurationPage(list);
                }
                // Ammo Configuration
                else if (_page3)
                {
                    DrawAmmoConfigurationPage(list, inRect);
                }

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                if (_minorChangesMade)
                {
                    _filterAmmoTrue = false;
                    _filterAmmoFalse = false;
                    _filterCatTrue = false;
                    _filterCatFalse = false;
                    _setAllAmmoRequiredTrue = false;
                    _setAllAmmoRequiredFalse = false;
                    _setAllCatTrue = false;
                    _setAllCatFalse = false;
                    _minorChangesMade = false;
                }

                if (_changesMade)
                {
                    AmmoLogic.FixCategoryWeaponDictionaries();
                    AmmoSettingsIO.Save();
                    _filterAmmoTrue = false;
                    _filterAmmoFalse = false;
                    _filterCatTrue = false;
                    _filterCatFalse = false;
                    _changesMade = false;
                }

                list.End();
                this.Write();
            }
            catch (Exception ex)
            {
                if (DebugLogs)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        private void DrawGeneralSettingsPage(Listing_Standard list)
        {
            Text.Font = GameFont.Medium;
            var headerRect = list.GetRect(_lineHeight * 1.5f);
            Widgets.Label(headerRect, LTS_Systems.Language.Translate.GeneralSettings);
            Text.Font = GameFont.Small;

            list.DrawCheckbox(_lineHeight, ref _useAmmoPerBullet, Translate.PerBullet, Translate.PerBulletDesc);
            list.DrawCheckbox(_lineHeight, ref _hideMultipleGizmos, Translate.HideMultipleGizmos,
                Translate.HideMultipleGizmosDesc);
            list.DrawCheckbox(_lineHeight, ref _useCompactGizmo, Translate.LtsUseWiderGizmo,
                Translate.LtsUseWiderGizmoDesc);
            list.DrawCheckbox(_lineHeight, ref _npcUseAmmo, Translate.NpcUseAmmo, Translate.NpcUseAmmoDesc);
            list.DrawCheckbox(_lineHeight, ref _debugLogs, LTS_Systems.Language.Translate.DebuggingMessages);

            var npcSpawnAmmoLabelRect = list.GetRect(_lineHeight);
            var npcSpawnAmmoSliderRect = list.GetRect(_lineHeight);
            Widgets.Label(npcSpawnAmmoLabelRect, Translate.NpcMaxAmmo(_range.min, _range.max));
            Widgets.IntRange(npcSpawnAmmoSliderRect, (int)npcSpawnAmmoSliderRect.y, ref _range, 10);
        }

        private void DrawKitConfigurationPage(Listing_Standard list)
        {
            Text.Font = GameFont.Medium;
            var headerRect = list.GetRect(_lineHeight * 1.5f);
            Widgets.Label(headerRect, Translate.SettingsKitsConfiguration);
            Text.Font = GameFont.Small;

            if (_chosenKit == null)
            {
                _chosenKit = AvailableKits.First();
            }

            if (!BagSettingsDictionary.TryGetValue(_chosenKit.defName, out var bags))
            {
                return;
            }

            // Kit Configuration Label
            Widgets.Label(list.GetRect(_lineHeight), Translate.ChangeBag);
            // Dropdown for Kit Selection
            var configureKitRect = list.GetRect(_lineHeight).LeftHalf();
            configureKitRect.width -= 24; // Adjust for extra spacing
            configureKitRect.SplitVertically(configureKitRect.width / 4, out var configureKitLeftRect,
                out var configureKitRightRect);

            Widgets.Label(configureKitLeftRect, Translate.ConfigureKit);
            Widgets.Dropdown(configureKitRightRect.LeftHalf(), _chosenKit, (selectedThing) => _ = selectedThing,
                GenerateKitCategoryMenu, _chosenKit?.LabelCap);

            // Add Slot Button
            if (Widgets.ButtonText(configureKitRightRect.RightHalf(), Translate.AddSlot))
            {
                if (_chosenKit?.comps.FirstOrDefault(x => x is CompProps_Kit) is CompProps_Kit kitProp)
                {
                    bags.AmmoCapacities.Add(kitProp.ammoCapacity.First()); // Add default ammo capacity from kit
                    _changesMade = true;
                }
            }

            // Display Ammo Capacities for the Selected Kit
            for (var i = 0; i < bags.AmmoCapacities.Count; i++)
            {
                var val = bags.AmmoCapacities[i];
                var buffer = val.ToString();
                var kitRect = list.GetRect(_lineHeight).LeftHalf();

                // Split the UI elements for each ammo capacity
                kitRect.SplitVertically(kitRect.width - 24, out var leftPart, out var rightPart);
                leftPart.SplitVertically(leftPart.width * 0.4f, out var leftPartLabel, out var leftPartField);

                // Label for "Bag #"
                Widgets.Label(leftPartLabel, Translate.BagNumber(i + 1));

                // Editable TextField for Ammo Capacity
                Widgets.TextFieldNumeric(leftPartField, ref val, ref buffer, 1, 999);

                // Detect if the value has changed and update accordingly
                if (val != bags.AmmoCapacities[i])
                {
                    _changesMade = true;
                    bags.AmmoCapacities[i] = val;
                }

                // Remove Button for Capacity
                if (Widgets.ButtonImage(rightPart, ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss"))
                    && bags.AmmoCapacities.Count > 1)
                {
                    bags.AmmoCapacities.RemoveAt(i);
                    _changesMade = true;
                }
            }
        }

        private void DrawAmmoConfigurationPage(Listing_Standard list, Rect inRect)
        {
            Text.Font = GameFont.Medium;
            DrawHeaderSection(list, inRect);

            if (AmmoCategoryDefs.Any())
            {
                if (_chosenCategory == null)
                {
                    _chosenCategory = AmmoCategoryDefs.First();
                }

                if (_chosenCategory == null)
                {
                    return;
                }

                this.DrawAmmoCategorySection(list);
                list.GapLine(2);
                this.DrawFilterOptions(list);
                list.GapLine(2);
                this.DrawMainSection(list);
            }
            else
            {
                list.Label(Translate.NoAmmoCategories);
            }
        }

        private void DrawHeaderSection(Listing_Standard list, Rect inRect)
        {
            var headerRect = list.GetRect(_lineHeight * 1.5f);
            headerRect.SplitVertically(inRect.width / 1.5f,
                out var headerRectLeft,
                out var headerRectRight);

            Widgets.Label(headerRectLeft, Translate.SettingsAmmoConfiguration);
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(headerRectRight.LeftHalf().ContractedBy(0, Margin),
                    Translate.SettingsSaveAsDefault))
            {
                var dialogConfirm = new DialogConfirm(AmmoSettingsIO.SaveAmmoDefault);
                Find.WindowStack.Add(dialogConfirm);
                _changesMade = true;
            }

            if (Widgets.ButtonText(headerRectRight.RightHalf().ContractedBy(0, Margin),
                    Translate.SettingsLoadDefault))
            {
                var dialogConfirm = new DialogConfirm(AmmoSettingsIO.LoadAmmoDefault);
                Find.WindowStack.Add(dialogConfirm);
                _changesMade = true;
            }
        }

        private void DrawAmmoCategorySection(Listing_Standard list)
        {
            var configureCategoryRect = list.GetRect(_lineHeight).LeftPart(0.4f);

            configureCategoryRect.SplitVertically(configureCategoryRect.width * 0.4f,
                out var configureCategoryLeftRect,
                out var configureCategoryRightRect);

            Widgets.Label(configureCategoryLeftRect, Translate.AvailableAmmoCategory);
            Widgets.Dropdown(configureCategoryRightRect, _chosenCategory,
                selectedThing => _ = selectedThing, GenerateAmmoCategoryMenu,
                _chosenCategory.LabelCap);

            Widgets.Label(list.GetRect(_lineHeight), Translate.AvailableAmmoCategoryDesc);
            var ammoRect = list.GetRect(_lineHeight * 3f);

            for (var i = 0; i < _chosenCategory.ammoDefs.Count; i++)
            {
                var ammoDef = DefDatabase<ThingDef>.GetNamed(_chosenCategory.ammoDefs[i]);
                var recImage = new Rect((ammoRect.x + i * _lineHeight * 3f), ammoRect.y,
                    _lineHeight * 3, _lineHeight * 3).ContractedBy(2);

                GenUI.DrawTextureWithMaterial(recImage, Command.BGTex, null);
                Widgets.DrawTextureFitted(recImage, (Texture2D)ammoDef.graphic.MatSingle.mainTexture,
                    0.95f);

                if (!Mouse.IsOver(recImage))
                {
                    continue;
                }

                Widgets.DrawHighlight(recImage);
                TooltipHandler.TipRegion(recImage, ammoDef.label + "\n" + ammoDef.description);
            }
        }

        private void DrawFilterOptions(Listing_Standard list)
        {
            list.GapLine(2);

            var filterLabelsRect = list.GetRect(_lineHeight);
            filterLabelsRect.SplitVertically(275, out var filterOptionsLabelsRect, out var filterLabelsRightRect);

            filterLabelsRightRect.SplitVertically(filterLabelsRightRect.width * 0.52f,
                out var filterWeaponsLabelRect, out var filterWeaponsRightRect);

            filterWeaponsRightRect.SplitVertically(filterWeaponsRightRect.width * 0.55f,
                out var filterCategoryLabelRect, out var filterAmmoRequirementLabelRect);

            filterCategoryLabelRect.SplitVertically(filterCategoryLabelRect.width - _lineHeight,
                out var filterCategoryTextRect,
                out var filterCategoryImageRect);

            filterAmmoRequirementLabelRect.SplitVertically(
                filterAmmoRequirementLabelRect.width - _lineHeight,
                out var filterAmmoRequirementTextRect,
                out var filterAmmoRequirementImageRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(filterOptionsLabelsRect, LTS_Systems.Language.Translate.FilterOptions + ":");
            Widgets.Label(filterWeaponsLabelRect, Translate.AmmoWeaponsOptions);

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(filterWeaponsLabelRect, Translate.AmmoWeaponsBaseDamage);
            Widgets.LabelFit(filterAmmoRequirementTextRect, Translate.AmmoUsageOptions);
            if (Widgets.ButtonImage(filterAmmoRequirementImageRect,
                    _filterAmmoTrue ? _filterAmmoFalse ? PartialIcon : TrueIcon : FalseIcon))
            {
                _minorChangesMade = true;
                if (_filterAmmoFalse)
                {
                    _setAllAmmoRequiredTrue = true;
                    _changesMade = true;
                }
                else
                {
                    _setAllAmmoRequiredFalse = true;
                    _changesMade = true;
                }
            }

            Widgets.LabelFit(filterCategoryTextRect,
                Translate.AmmoCategoryOptions(_chosenCategory.LabelCap));

            if (Widgets.ButtonImage(filterCategoryImageRect,
                    _filterCatTrue ? _filterCatFalse ? PartialIcon : TrueIcon : FalseIcon))
            {
                _minorChangesMade = true;
                if (_filterCatFalse)
                {
                    _setAllCatTrue = true;
                    _changesMade = true;
                }
                else
                {
                    _setAllCatFalse = true;
                    _changesMade = true;
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawMainSection(Listing_Standard list)
        {
            var optionsRect = list.GetRect((_lineHeight * 13f) - Margin * 2);
            Widgets.DrawMenuSection(optionsRect);
            optionsRect.SplitVertically(250,
                out var filterRect,
                out var weaponsInRect);

            var listFilter = new Listing_Standard();
            listFilter.Begin(filterRect);

            DrawFilterSection(listFilter);
            DrawWeaponsSection(weaponsInRect);
        }

        private void DrawFilterSection(Listing_Standard list)
        {
            //Filter Search
            var searchRect = list.GetRect(_lineHeight * 1.4f).ContractedBy(Margin);
            searchRect.SplitVertically(searchRect.width * 0.30f,
                out var filterSearchTextRect,
                out var filterSearchInputRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(filterSearchTextRect, LTS_Systems.Language.Translate.Search + ":");
            Text.Anchor = TextAnchor.UpperLeft;
            var tempSearchFilter = _searchFilter;
            _searchFilter = Widgets.TextField(filterSearchInputRect, _searchFilter);

            // Filter Mods
            var modRect = list.GetRect(_lineHeight * 1.4f).ContractedBy(Margin);
            modRect.SplitVertically(modRect.width * 0.3f,
                out var modLeftRect,
                out var modRightRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(modLeftRect, LTS_Systems.Language.Translate.From + ":");
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Dropdown(modRightRect, _chosenMod,
                selectedThing => _ = selectedThing, GenerateModCategoryMenu,
                _chosenMod == null ? LTS_Systems.Language.Translate.All : _chosenMod.Name);

            //Filter Toggles
            var tempNeolithic = _neolithic;
            var tempMedieval = _medieval;
            var tempIndustrial = _industrial;
            var tempSpacer = _spacer;
            var tempUltra = _ultra;
            var tempArcho = _archotech;
            var tempUnused = _unused;

            var unusedRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(unusedRect,
                Translate.AmmoFilterAllUnused, ref _unused);

            var neolithicRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(neolithicRect,
                ((TechLevel)2).ToStringHuman().CapitalizeFirst(), ref _neolithic);

            var medievalRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(medievalRect,
                ((TechLevel)3).ToStringHuman().CapitalizeFirst(), ref _medieval);

            var industrialRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(industrialRect,
                ((TechLevel)4).ToStringHuman().CapitalizeFirst().CapitalizeFirst(), ref _industrial);

            var spacerRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(spacerRect,
                ((TechLevel)5).ToStringHuman().CapitalizeFirst(), ref _spacer);

            var ultraRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(ultraRect,
                ((TechLevel)6).ToStringHuman().CapitalizeFirst(), ref _ultra);

            var archotechRect = list.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
            Widgets.CheckboxLabeled(archotechRect,
                ((TechLevel)7).ToStringHuman().CapitalizeFirst(), ref _archotech);

            if (tempSearchFilter != _searchFilter
                || tempNeolithic != _neolithic
                || tempMedieval != _medieval
                || tempIndustrial != _industrial
                || tempSpacer != _spacer
                || tempUltra != _ultra
                || tempArcho != _archotech
                || tempUnused != _unused)
            {
                _minorChangesMade = true;
            }

            list.End();
        }

        private void DrawWeaponsSection(Rect weaponsInRect)
        {
            Widgets.DrawMenuSection(weaponsInRect);
            var weaponsOutRect = new Rect(weaponsInRect.ContractedBy(Margin));
            //All viable weapons according to filter
            var weapons = AvailableProjectileWeapons.Where(thingDef =>
                    thingDef.label.ToLower().Contains(_searchFilter.ToLower())
                    && (_chosenMod == null || thingDef.modContentPack == _chosenMod)
                    && (
                        (thingDef.techLevel == TechLevel.Neolithic && _neolithic)
                        || (thingDef.techLevel == TechLevel.Medieval && _medieval)
                        || (thingDef.techLevel == TechLevel.Industrial && _industrial)
                        || (thingDef.techLevel == TechLevel.Spacer && _spacer)
                        || (thingDef.techLevel == TechLevel.Ultra && _ultra)
                        || (thingDef.techLevel == TechLevel.Archotech && _archotech))
                    && ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out var exemption)
                    && (!_unused || exemption)
                )
                .ToList();

            var num2 = weapons.EnumerableNullOrEmpty()
                ? 1
                : weapons.Count() * _lineHeight;

            if (num2 < weaponsOutRect.height)
            {
                num2 = weaponsOutRect.height;
            }

            //Weapons
            var weaponsViewRect = new Rect(0f, 0f, weaponsOutRect.width - Margin * 4, num2);
            Widgets.BeginScrollView(weaponsOutRect, ref _scrollWeaponsPos, weaponsViewRect);
            var weaponsList = new Listing_Standard(weaponsInRect, () => _scrollWeaponsPos);
            weaponsList.Begin(weaponsViewRect);

            CategoryWeaponDictionary.TryGetValue(_chosenCategory.defName,
                out var chosenCategoryDictionary);

            //Populate list
            foreach (var thingDef in weapons)
            {
                chosenCategoryDictionary.TryGetValue(thingDef.defName, out var canUseAmmoCategory);
                ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out var weaponIsExempt);
                if (_unused && !weaponIsExempt)
                {
                    continue;
                }

                var weaponRect = weaponsList.GetRect(_lineHeight);
                weaponRect.SplitVertically(weaponRect.width * 0.55f,
                    out var weaponTitleRect,
                    out var weaponOptionsRect);

                weaponTitleRect.SplitVertically(25,
                    out var iconRect,
                    out var weaponLabelRect);

                weaponOptionsRect.SplitVertically(weaponOptionsRect.width * 0.55f,
                    out var categoryUsageRect,
                    out var ammoUsageRect);

                categoryUsageRect.SplitVertically(categoryUsageRect.width - _lineHeight,
                    out var categoryUsageLabelRect,
                    out var categoryUsageIconRect);

                ammoUsageRect.SplitVertically(ammoUsageRect.width - _lineHeight,
                    out var ammoUsageLabelRect,
                    out var ammoUsageIconRect);

                Widgets.DrawTextureFitted(iconRect, Widgets.GetIconFor(thingDef), 0.95f);
                Widgets.Label(weaponLabelRect, thingDef.LabelCap);
                Widgets.DrawHighlightIfMouseover(weaponRect);

                Text.Anchor = TextAnchor.MiddleRight;
                var shoot = thingDef.Verbs.FirstOrDefault(
                    v => v.defaultProjectile != null);

                Widgets.Label(weaponTitleRect,
                    shoot?.defaultProjectile?.projectile?.GetDamageAmount(1f).ToString() ?? "x");

                var val = canUseAmmoCategory;
                //These are for showing header icon
                if (val)
                {
                    _filterCatTrue = true;
                }
                else
                {
                    _filterCatFalse = true;
                }

                var val2 = weaponIsExempt;
                if (val2)
                {
                    _filterAmmoFalse = true;
                }
                else
                {
                    _filterAmmoTrue = true;
                }


                if (_setAllCatFalse)
                {
                    chosenCategoryDictionary.SetOrAdd(thingDef.defName, false);
                    _changesMade = true;
                }
                else if (_setAllCatTrue)
                {
                    chosenCategoryDictionary.SetOrAdd(thingDef.defName, true);
                    ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
                    _changesMade = true;
                }

                Text.Font = GameFont.Tiny;
                Widgets.Label(categoryUsageLabelRect,
                    Translate.AmmoCategoryOptions(_chosenCategory.LabelCap));

                if (Widgets.ButtonImage(categoryUsageIconRect, val ? TrueIcon : FalseIcon))
                {
                    val = !val;
                    chosenCategoryDictionary.SetOrAdd(thingDef.defName, val);
                    if (val)
                    {
                        ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
                    }

                    _changesMade = true;
                }

                if (_setAllAmmoRequiredFalse)
                {
                    ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, true);
                    //set all weapons to not require any ammo in any categories.
                    foreach (var categoryDictionaries in
                             CategoryWeaponDictionary)
                    {
                        categoryDictionaries.Value.SetOrAdd(thingDef.defName, false);
                    }

                    _changesMade = true;
                }
                else if (_setAllAmmoRequiredTrue)
                {
                    ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
                    chosenCategoryDictionary.SetOrAdd(thingDef.defName, true);
                    _changesMade = true;
                }

                Widgets.Label(ammoUsageLabelRect, Translate.AmmoUsageOptions);
                if (!Widgets.ButtonImage(ammoUsageIconRect, val2 ? FalseIcon : TrueIcon))
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    continue;
                }

                val2 = !val2;
                ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, val2);
                //If true, set all weapons to not require any ammo in any categories.
                if (val2)
                {
                    foreach (var categoryDictionaries in
                             CategoryWeaponDictionary)
                    {
                        categoryDictionaries.Value.SetOrAdd(thingDef.defName, false);
                    }
                }
                else
                {
                    chosenCategoryDictionary.SetOrAdd(thingDef.defName, true);
                }

                _changesMade = true;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            weaponsList.End();

            Widgets.EndScrollView();
        }

        private static IEnumerable<Widgets.DropdownMenuElement<AmmoCategoryDef>> GenerateAmmoCategoryMenu(
            AmmoCategoryDef def)
        {
            using (var enumerator = AmmoCategoryDefs.ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var ammo = enumerator.Current;
                    yield return new Widgets.DropdownMenuElement<AmmoCategoryDef>
                    {
                        option = new FloatMenuOption(ammo?.LabelCap, delegate { _chosenCategory = ammo; }),
                        payload = ammo
                    };
                }
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateKitCategoryMenu(ThingDef def)
        {
            using (var enumerator = AvailableKits.ToList().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var kit = enumerator.Current;
                    yield return new Widgets.DropdownMenuElement<ThingDef>
                    {
                        option = new FloatMenuOption(kit?.LabelCap, delegate { _chosenKit = kit; }), payload = kit
                    };
                }
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<ModContentPack>> GenerateModCategoryMenu(
            ModContentPack def)
        {
            yield return new Widgets.DropdownMenuElement<ModContentPack>
            {
                option = new FloatMenuOption(LTS_Systems.Language.Translate.All, delegate { _chosenMod = null; }),
                payload = null
            };

            using (var enumerator = LoadedModManager.RunningMods.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var mod = enumerator.Current;
                    if (AvailableProjectileWeapons.FirstOrDefault(
                            x => x.modContentPack == mod)
                        != null)
                    {
                        yield return new Widgets.DropdownMenuElement<ModContentPack>
                        {
                            option = new FloatMenuOption(mod?.Name, delegate { _chosenMod = mod; }), payload = mod
                        };
                    }
                }
            }
        }
    }
}