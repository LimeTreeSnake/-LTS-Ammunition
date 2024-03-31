using System;
using System.Collections.Generic;
using System.Linq;
using Ammunition.Components;
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
			Settings.Initialize();
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
		public static Texture2D PartialIcon => Icons.Partial;
		public static Texture2D DropIcon => Icons.Down;
		public static Texture2D InfoIcon => Icons.Info;

		#region Fields

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

		#endregion Fields


		public static bool HideMultipleGizmos => _hideMultipleGizmos;
		public static IntRange Range => _range;
		public static bool UseAmmoPerBullet => _useAmmoPerBullet;
		public static bool UseCompactGizmo => _useCompactGizmo;
		public static bool NpcUseAmmo => _npcUseAmmo;
		public static bool DebugLogs => _debugLogs;

		/// <summary>
		/// CategoryDictionary -> WeaponDefDictionary (Can use this category (True) or not (False)
		/// </summary>
		public static Dictionary<string, Dictionary<string, bool>> CategoryWeaponDictionary;

		//Contains all viable weapons monitored, if it's not in this list, then it's not a viable weapon.
		/// <summary>
		/// If set to true, then the weapon is exempt.
		/// </summary>
		public static Dictionary<string, bool> ExemptionWeaponDictionary;

		public static Dictionary<string, List<int>> BagSettingsDictionary;

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

		public static void Initialize()
		{
			if (CategoryWeaponDictionary == null)
			{
				CategoryWeaponDictionary = new Dictionary<string, Dictionary<string, bool>>();
			}

			if (ExemptionWeaponDictionary == null)
			{
				ExemptionWeaponDictionary = new Dictionary<string, bool>();
			}

			if (BagSettingsDictionary == null)
			{
				BagSettingsDictionary = new Dictionary<string, List<int>>();
			}
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
				Rect topSettingsRect = list.GetRect(_lineHeight * 2);
				topSettingsRect.SplitVertically(inRect.width / 1.5f,
					out Rect topSettingsRectLeft,
					out Rect topSettingsRectRight);

				topSettingsRectLeft.SplitVertically(topSettingsRectLeft.width / 3,
					out Rect generalSettingsRect,
					out Rect otherSettingsRectRight);

				otherSettingsRectRight.SplitVertically(otherSettingsRectRight.width / 2,
					out Rect kitsSettingsRect,
					out Rect ammoSettingsRect);

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
					Text.Font = GameFont.Medium;
					Rect headerRect = list.GetRect(_lineHeight * 1.5f);
					Widgets.Label(headerRect, LTS_Systems.Language.Translate.GeneralSettings);
					Text.Font = GameFont.Small;

					Rect perBulletRect = list.GetRect(_lineHeight).LeftHalf().LeftHalf();
					Widgets.CheckboxLabeled(perBulletRect, Translate.PerBullet, ref _useAmmoPerBullet);
					if (Mouse.IsOver(perBulletRect))
					{
						Widgets.DrawHighlight(perBulletRect);
					}

					TooltipHandler.TipRegion(perBulletRect, Translate.PerBulletDesc);

					list.Gap(2);
					Rect hideGizmoRect = list.GetRect(_lineHeight).LeftHalf().LeftHalf();
					Widgets.CheckboxLabeled(hideGizmoRect, Translate.HideMultipleGizmos, ref _hideMultipleGizmos);
					if (Mouse.IsOver(hideGizmoRect))
					{
						Widgets.DrawHighlight(hideGizmoRect);
					}

					TooltipHandler.TipRegion(hideGizmoRect, Translate.HideMultipleGizmosDesc);

					list.Gap(2);
					Rect useCompactGizmoRect = list.GetRect(_lineHeight).LeftHalf().LeftHalf();
					Widgets.CheckboxLabeled(useCompactGizmoRect, Translate.LtsUseWiderGizmo, ref _useCompactGizmo);
					if (Mouse.IsOver(useCompactGizmoRect))
					{
						Widgets.DrawHighlight(useCompactGizmoRect);
					}

					TooltipHandler.TipRegion(useCompactGizmoRect, Translate.LtsUseWiderGizmoDesc);

					list.Gap(2);
					Rect npcUsesAmmoRect = list.GetRect(_lineHeight).LeftHalf().LeftHalf();
					Widgets.CheckboxLabeled(npcUsesAmmoRect, Translate.NpcUseAmmo, ref _npcUseAmmo);
					if (Mouse.IsOver(npcUsesAmmoRect))
					{
						Widgets.DrawHighlight(npcUsesAmmoRect);
					}

					TooltipHandler.TipRegion(npcUsesAmmoRect, Translate.NpcUseAmmoDesc);

					list.Gap(2);
					Rect debuggingRect = list.GetRect(_lineHeight).LeftHalf().LeftHalf();
					Widgets.CheckboxLabeled(debuggingRect, LTS_Systems.Language.Translate.DebuggingMessages,
						ref _debugLogs);

					if (Mouse.IsOver(debuggingRect))
					{
						Widgets.DrawHighlight(debuggingRect);
					}

					list.Gap(2);
					Rect npcSpawnAmmoLabelRect = list.GetRect(_lineHeight);
					Rect npcSpawnAmmoSliderRect = list.GetRect(_lineHeight);
					Widgets.Label(npcSpawnAmmoLabelRect, Translate.NpcMaxAmmo(_range.min, _range.max));
					Widgets.IntRange(npcSpawnAmmoSliderRect, (int)npcSpawnAmmoSliderRect.y, ref _range, 10);
				}
				// Kit Configuration
				else if (_page2)
				{
					Text.Font = GameFont.Medium;
					Rect headerRect = list.GetRect(_lineHeight * 1.5f);
					Widgets.Label(headerRect, Translate.SettingsKitsConfiguration);
					Text.Font = GameFont.Small;
					if (_chosenKit == null)
					{
						_chosenKit = AmmoLogic.AvailableKits.First();
					}

					if (BagSettingsDictionary.TryGetValue(_chosenKit.defName, out List<int> bags))
					{
						Widgets.Label(list.GetRect(_lineHeight), Translate.ChangeBag);

						Rect configureKitRect = list.GetRect(_lineHeight).LeftHalf();
						configureKitRect.width -= 24;
						configureKitRect.SplitVertically(configureKitRect.width / 4,
							out Rect configureKitLeftRect,
							out Rect configureKitRightRect);

						Widgets.Label(configureKitLeftRect, Translate.ConfigureKit);
						Widgets.Dropdown(configureKitRightRect.LeftHalf(), _chosenKit,
							(selectedThing) => _ = selectedThing, GenerateKitCategoryMenu,
							_chosenKit?.LabelCap);

						if (Widgets.ButtonText(configureKitRightRect.RightHalf(), Translate.AddSlot))
						{
							if (_chosenKit?.comps.FirstOrDefault(x => x is CompProps_Kit) is CompProps_Kit kitProp)
							{
								bags.Add(kitProp.ammoCapacity.First());
								_changesMade = true;
							}
						}

						list.Gap(2);
						for (int i = 0; i < bags.Count; i++)
						{
							int val = bags[i];
							string buffer = val.ToString();
							Rect kitRect = list.GetRect(_lineHeight).LeftHalf();

							kitRect.SplitVertically(kitRect.width - 24,
								out Rect leftPart,
								out Rect rightPart);

							leftPart.SplitVertically(leftPart.width * 0.4f,
								out Rect leftPartLabel,
								out Rect leftPartField);

							Widgets.Label(leftPartLabel, Translate.BagNumber(i + 1));
							Widgets.TextFieldNumeric(leftPartField, ref val, ref buffer, 1, 999);
							if (val != bags[i])
							{
								_changesMade = true;
								bags[i] = val;
							}

							if (Widgets.ButtonImage(rightPart, ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss")) &&
							    bags.Count > 1)
							{
								bags.RemoveAt(i);
								_changesMade = true;
							}
						}
					}
				}
				// Ammo Configuration
				else if (_page3)
				{
					Text.Font = GameFont.Medium;
					Rect headerRect = list.GetRect(_lineHeight * 1.5f);
					headerRect.SplitVertically(inRect.width / 1.5f,
						out Rect headerRectLeft,
						out Rect headerRectRight);

					Widgets.Label(headerRectLeft, Translate.SettingsAmmoConfiguration);
					Text.Font = GameFont.Small;

					if (Widgets.ButtonText(headerRectRight.LeftHalf().ContractedBy(0, Margin),
						    Translate.SettingsSaveAsDefault))
					{
						var dialogConfirm = new DialogConfirm(AmmoLogic.SaveAmmoDefault);
						Find.WindowStack.Add(dialogConfirm);
						_changesMade = true;
					}

					if (Widgets.ButtonText(headerRectRight.RightHalf().ContractedBy(0, Margin),
						    Translate.SettingsLoadDefault))
					{
						var dialogConfirm = new DialogConfirm(AmmoLogic.LoadAmmoDefault);
						Find.WindowStack.Add(dialogConfirm);
						_changesMade = true;
					}

					if (AmmoLogic.AmmoCategoryDefs.Any())
					{
						if (_chosenCategory == null)
						{
							_chosenCategory = AmmoLogic.AmmoCategoryDefs.First();
						}

						if (_chosenCategory != null)
						{
							Rect configureCategoryRect = list.GetRect(_lineHeight).LeftPart(0.4f);

							configureCategoryRect.SplitVertically(configureCategoryRect.width * 0.4f,
								out Rect configureCategoryLeftRect,
								out Rect configureCategoryRightRect);

							Widgets.Label(configureCategoryLeftRect, Translate.AvailableAmmoCategory);
							Widgets.Dropdown(configureCategoryRightRect, _chosenCategory,
								selectedThing => _ = selectedThing, GenerateAmmoCategoryMenu,
								_chosenCategory.LabelCap);

							Widgets.Label(list.GetRect(_lineHeight), Translate.AvailableAmmoCategoryDesc);
							Rect ammoRect = list.GetRect(_lineHeight * 3f);

							for (int i = 0; i < _chosenCategory.ammoDefs.Count; i++)
							{
								ThingDef ammoDef = DefDatabase<ThingDef>.GetNamed(_chosenCategory.ammoDefs[i]);
								Rect recImage = new Rect((ammoRect.x + i * _lineHeight * 3f), ammoRect.y,
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

							list.GapLine(2);

							Rect filterLabelsRect = list.GetRect(_lineHeight);
							filterLabelsRect.SplitVertically(275,
								out Rect filterOptionsLabelsRect,
								out Rect filterLabelsRightRect);

							filterLabelsRightRect.SplitVertically(filterLabelsRightRect.width * 0.52f,
								out Rect filterWeaponsLabelRect,
								out Rect filterWeaponsRightRect);

							filterWeaponsRightRect.SplitVertically(filterWeaponsRightRect.width * 0.55f,
								out Rect filterCategoryLabelRect,
								out Rect filterAmmoRequirementLabelRect);

							filterCategoryLabelRect.SplitVertically(filterCategoryLabelRect.width - _lineHeight,
								out Rect filterCategoryTextRect,
								out Rect filterCategoryImageRect);

							filterAmmoRequirementLabelRect.SplitVertically(
								filterAmmoRequirementLabelRect.width - _lineHeight,
								out Rect filterAmmoRequirementTextRect,
								out Rect filterAmmoRequirementImageRect);

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

							list.GapLine(2);

							//Main Configuration Window
							Rect optionsRect = list.GetRect((_lineHeight * 13f) - Margin * 2);
							Widgets.DrawMenuSection(optionsRect);
							optionsRect.SplitVertically(250,
								out Rect filterRect,
								out Rect weaponsInRect);

							#region Filters

							var listFilter = new Listing_Standard();
							listFilter.Begin(filterRect);

							//Filter Search
							Rect searchRect = listFilter.GetRect(_lineHeight * 1.4f).ContractedBy(Margin);
							searchRect.SplitVertically(searchRect.width * 0.30f,
								out Rect filterSearchTextRect,
								out Rect filterSearchInputRect);

							Text.Anchor = TextAnchor.MiddleLeft;
							Widgets.Label(filterSearchTextRect, LTS_Systems.Language.Translate.Search + ":");
							Text.Anchor = TextAnchor.UpperLeft;
							string tempSearchFilter = _searchFilter;
							_searchFilter = Widgets.TextField(filterSearchInputRect, _searchFilter);

							// Filter Mods
							Rect modRect = listFilter.GetRect(_lineHeight * 1.4f).ContractedBy(Margin);
							modRect.SplitVertically(modRect.width * 0.3f,
								out Rect modLeftRect,
								out Rect modRightRect);

							Text.Anchor = TextAnchor.MiddleLeft;
							Widgets.Label(modLeftRect, LTS_Systems.Language.Translate.From + ":");
							Text.Anchor = TextAnchor.UpperLeft;
							Widgets.Dropdown(modRightRect, _chosenMod,
								selectedThing => _ = selectedThing, GenerateModCategoryMenu,
								_chosenMod == null ? LTS_Systems.Language.Translate.All : _chosenMod.Name);

							//Filter Toggles
							bool tempNeolithic = _neolithic;
							bool tempMedieval = _medieval;
							bool tempIndustrial = _industrial;
							bool tempSpacer = _spacer;
							bool tempUltra = _ultra;
							bool tempArcho = _archotech;
							bool tempUnused = _unused;

							Rect unusedRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(unusedRect,
								Translate.AmmoFilterAllUnused, ref _unused);

							Rect neolithicRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(neolithicRect,
								((TechLevel)2).ToStringHuman().CapitalizeFirst(), ref _neolithic);

							Rect medievalRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(medievalRect,
								((TechLevel)3).ToStringHuman().CapitalizeFirst(), ref _medieval);

							Rect industrialRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(industrialRect,
								((TechLevel)4).ToStringHuman().CapitalizeFirst().CapitalizeFirst(), ref _industrial);

							Rect spacerRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(spacerRect,
								((TechLevel)5).ToStringHuman().CapitalizeFirst(), ref _spacer);

							Rect ultraRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(ultraRect,
								((TechLevel)6).ToStringHuman().CapitalizeFirst(), ref _ultra);

							Rect archotechRect = listFilter.GetRect(_lineHeight * 1.2f).ContractedBy(Margin);
							Widgets.CheckboxLabeled(archotechRect,
								((TechLevel)7).ToStringHuman().CapitalizeFirst(), ref _archotech);

							if (tempSearchFilter != _searchFilter ||
							    tempNeolithic != _neolithic ||
							    tempMedieval != _medieval ||
							    tempIndustrial != _industrial ||
							    tempSpacer != _spacer ||
							    tempUltra != _ultra ||
							    tempArcho != _archotech ||
							    tempUnused != _unused)
							{
								_minorChangesMade = true;
							}

							listFilter.End();

							#endregion //Filters

							#region WeaponConfiguration

							Widgets.DrawMenuSection(weaponsInRect);
							var weaponsOutRect = new Rect(weaponsInRect.ContractedBy(Margin));
							//All viable weapons according to filter
							var weapons = AmmoLogic.AvailableProjectileWeapons.Where(thingDef =>
									thingDef.label.ToLower().Contains(_searchFilter.ToLower()) &&
									(_chosenMod == null || thingDef.modContentPack == _chosenMod) &&
									(
										(thingDef.techLevel == TechLevel.Neolithic && _neolithic) ||
										(thingDef.techLevel == TechLevel.Medieval && _medieval) ||
										(thingDef.techLevel == TechLevel.Industrial && _industrial) ||
										(thingDef.techLevel == TechLevel.Spacer && _spacer) ||
										(thingDef.techLevel == TechLevel.Ultra && _ultra) ||
										(thingDef.techLevel == TechLevel.Archotech && _archotech)) &&
									ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out bool exemption) &&
									(!_unused || exemption)
								)
								.ToList();

							float num2 = weapons.EnumerableNullOrEmpty()
								? 1
								: weapons.Count() *
								  _lineHeight;

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
								out Dictionary<string, bool> chosenCategoryDictionary);

							//Populate list
							foreach (ThingDef thingDef in weapons)
							{
								chosenCategoryDictionary.TryGetValue(thingDef.defName, out bool canUseAmmoCategory);
								ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out bool weaponIsExempt);
								if (_unused && !weaponIsExempt)
								{
									continue;
								}

								Rect weaponRect = weaponsList.GetRect(_lineHeight);
								weaponRect.SplitVertically(weaponRect.width * 0.55f,
									out Rect weaponTitleRect,
									out Rect weaponOptionsRect);

								weaponTitleRect.SplitVertically(25,
									out Rect iconRect,
									out Rect weaponLabelRect);

								weaponOptionsRect.SplitVertically(weaponOptionsRect.width * 0.55f,
									out Rect categoryUsageRect,
									out Rect ammoUsageRect);

								categoryUsageRect.SplitVertically(categoryUsageRect.width - _lineHeight,
									out Rect categoryUsageLabelRect,
									out Rect categoryUsageIconRect);

								ammoUsageRect.SplitVertically(ammoUsageRect.width - _lineHeight,
									out Rect ammoUsageLabelRect,
									out Rect ammoUsageIconRect);

								Widgets.DrawTextureFitted(iconRect, Widgets.GetIconFor(thingDef), 0.95f);
								Widgets.Label(weaponLabelRect, thingDef.LabelCap);
								Widgets.DrawHighlightIfMouseover(weaponRect);

								Text.Anchor = TextAnchor.MiddleRight;
								VerbProperties shoot = thingDef.Verbs.FirstOrDefault(
									v => v.defaultProjectile != null);

								Widgets.Label(weaponTitleRect,
									shoot?.defaultProjectile?.projectile?.GetDamageAmount(1f).ToString() ?? "x");

								bool val = canUseAmmoCategory;
								//These are for showing header icon
								if (val)
								{
									_filterCatTrue = true;
								}
								else
								{
									_filterCatFalse = true;
								}

								bool val2 = weaponIsExempt;
								if (val2)
								{
									_filterAmmoFalse = true;
								}
								else
								{
									_filterAmmoTrue = true;
								}

								Text.Font = GameFont.Tiny;

								#region CanUseCurrentAmmo

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

								#endregion CanUseCurrentAmmo

								#region RequiresAmmo

								if (_setAllAmmoRequiredFalse)
								{
									ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, true);
									//set all weapons to not require any ammo in any categories.
									foreach (KeyValuePair<string, Dictionary<string, bool>> categoryDictionaries in
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
								if (Widgets.ButtonImage(ammoUsageIconRect, val2 ? FalseIcon : TrueIcon))
								{
									val2 = !val2;
									ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, val2);
									//If true, set all weapons to not require any ammo in any categories.
									if (val2)
									{
										foreach (KeyValuePair<string, Dictionary<string, bool>> categoryDictionaries in
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
								}

								#endregion

								Text.Font = GameFont.Small;
								Text.Anchor = TextAnchor.UpperLeft;
							}

							weaponsList.End();

							#endregion WeaponConfiguration

							Widgets.EndScrollView();


						}
					}
					else
					{
						list.Label(Translate.NoAmmoCategories);
					}

				}

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
					AmmoLogic.Save();
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

		private static IEnumerable<Widgets.DropdownMenuElement<AmmoCategoryDef>> GenerateAmmoCategoryMenu(
			AmmoCategoryDef def)
		{
			using (List<AmmoCategoryDef>.Enumerator enumerator = AmmoLogic.AmmoCategoryDefs.ToList().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					AmmoCategoryDef ammo = enumerator.Current;
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
			using (List<ThingDef>.Enumerator enumerator = AmmoLogic.AvailableKits.ToList().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ThingDef kit = enumerator.Current;
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

			using (IEnumerator<ModContentPack> enumerator = LoadedModManager.RunningMods.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ModContentPack mod = enumerator.Current;
					if (AmmoLogic.AvailableProjectileWeapons.FirstOrDefault(
						    x => x.modContentPack == mod) !=
					    null)
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