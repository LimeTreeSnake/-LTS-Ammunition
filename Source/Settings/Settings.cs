using System;
using System.Collections.Generic;
using System.Linq;
using Ammunition.Components;
using Ammunition.Defs;
using Ammunition.Language;
using Ammunition.Logic;
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

		#region Fields

		private static bool _useAmmoPerBullet = true;
		private static bool _hideMultipleGizmos = true;
		private static bool _useCompactGizmo;
		private static bool _npcUseAmmo = true;
		private static bool _debugLogs;
		private static IntRange _range = new IntRange(30, 60);
		private static AmmoCategoryDef _chosenCategory;
		private static ThingDef _chosenKit;
		private static bool _page1 = true, _page2, _page3;
		private static bool _changesMade, _minorChangesMade;
		private static bool _filterCatFalse, _filterCatTrue, _filterAmmoFalse, _filterAmmoTrue;
		private static bool _setAllAmmoTrue, _setAllAmmoFalse, _setAllCatTrue, _setAllCatFalse;

		private static bool
			_neolithic = true,
			_medieval = true,
			_industrial = true,
			_spacer = true,
			_ultra = true,
			_archotech = true;

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

		public static Dictionary<string, Dictionary<string, bool>> CategoryWeaponDictionary;

		//Contains all viable weapons monitored, if it's not in this list, then it's not a viable weapon.
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

				if (Widgets.ButtonText(generalSettingsRect.ContractedBy(0, Margin), Translate.SettingsGeneral))
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

				if (Widgets.ButtonText(topSettingsRectRight.RightHalf().BottomHalf(), Translate.DefaultSettings))
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
					Widgets.Label(headerRect, Translate.SettingsGeneral);
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
					Widgets.CheckboxLabeled(debuggingRect, Translate.AmmoDebugging, ref _debugLogs);
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

						Rect configureCategoryRect = list.GetRect(_lineHeight).LeftPart(0.4f);

						configureCategoryRect.SplitVertically(configureCategoryRect.width * 0.4f,
							out Rect configureCategoryLeftRect,
							out Rect configureCategoryRightRect);

						Widgets.Label(configureCategoryLeftRect, Translate.AvailableAmmoCategory);
						Widgets.Dropdown(configureCategoryRightRect, _chosenCategory,
							selectedThing => _ = selectedThing, GenerateAmmoCategoryMenu,
							_chosenCategory?.LabelCap);

						Widgets.Label(list.GetRect(_lineHeight), Translate.AvailableAmmoCategoryDesc);
						Rect ammoRect = list.GetRect(_lineHeight * 3f);

						if (_chosenCategory != null)
						{
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
								Widgets.LabelFit(recImage, ammoDef.label);
							}
						}

						list.GapLine(2);

						Rect filterLabelsRect = list.GetRect(_lineHeight);
						filterLabelsRect.SplitVertically(275,
							out Rect filterOptionsLabelsRect,
							out Rect filterLabelsRightRect);

						filterLabelsRightRect.SplitVertically(filterLabelsRightRect.width * 0.45f,
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

						Widgets.Label(filterOptionsLabelsRect, Translate.AmmoFilterOptions);
						Widgets.Label(filterWeaponsLabelRect, Translate.AmmoWeaponsOptions);

						Text.Anchor = TextAnchor.MiddleRight;
						Widgets.LabelFit(filterAmmoRequirementTextRect, Translate.AmmoUsageOptions);
						if (Widgets.ButtonImage(filterAmmoRequirementImageRect,
							    _filterAmmoTrue ? _filterAmmoFalse ? PartialIcon : TrueIcon : FalseIcon))
						{
							_minorChangesMade = true;
							if (_filterAmmoFalse)
							{
								_setAllAmmoTrue = true;
								_changesMade = true;
							}
							else
							{
								_setAllAmmoFalse = true;
								_changesMade = true;
							}
						}

						Widgets.LabelFit(filterCategoryTextRect,
							Translate.AmmoCategoryOptions(_chosenCategory?.LabelCap));

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

						//ADD BUTTON CHECK UNCHECK ALL
						list.GapLine(2);

						//Main
						Rect optionsRect = list.GetRect((_lineHeight * 12f) - Margin * 2);
						Widgets.DrawMenuSection(optionsRect);
						optionsRect.SplitVertically(250,
							out Rect filterRect,
							out Rect weaponsInRect);

						filterRect.ContractedBy(Margin)
							.SplitHorizontally(filterRect.height / 2,
								out Rect filterTopHalfRect,
								out Rect filterBottomHalfRect);

						//Search
						Rect searchRect = filterTopHalfRect.TopHalf().TopHalf().ContractedBy(2);
						searchRect.SplitVertically(searchRect.width * 0.30f,
							out Rect filterSearchTextRect,
							out Rect filterSearchInputRect);

						Text.Anchor = TextAnchor.MiddleLeft;
						Widgets.Label(filterSearchTextRect, Translate.AmmoSearchOptions);
						Text.Anchor = TextAnchor.UpperLeft;

						string tempSearchFilter = _searchFilter;
						_searchFilter = Widgets.TextField(filterSearchInputRect, _searchFilter);
						bool tempNeolithic = _neolithic;
						bool tempMedieval = _medieval;
						bool tempIndustrial = _industrial;
						bool tempSpacer = _spacer;
						bool tempUltra = _ultra;
						bool tempArcho = _archotech;
						Widgets.CheckboxLabeled(filterTopHalfRect.TopHalf().BottomHalf().ContractedBy(2),
							((TechLevel)2).ToStringHuman().CapitalizeFirst(), ref _neolithic);

						Widgets.CheckboxLabeled(filterTopHalfRect.BottomHalf().TopHalf().ContractedBy(2),
							((TechLevel)3).ToStringHuman().CapitalizeFirst(), ref _medieval);

						Widgets.CheckboxLabeled(filterTopHalfRect.BottomHalf().BottomHalf().ContractedBy(2),
							((TechLevel)4).ToStringHuman().CapitalizeFirst().CapitalizeFirst(), ref _industrial);

						Widgets.CheckboxLabeled(filterBottomHalfRect.TopHalf().TopHalf().ContractedBy(2),
							((TechLevel)5).ToStringHuman().CapitalizeFirst(), ref _spacer);

						Widgets.CheckboxLabeled(filterBottomHalfRect.TopHalf().BottomHalf().ContractedBy(2),
							((TechLevel)6).ToStringHuman().CapitalizeFirst(), ref _ultra);

						Widgets.CheckboxLabeled(filterBottomHalfRect.BottomHalf().TopHalf().ContractedBy(2),
							((TechLevel)7).ToStringHuman().CapitalizeFirst(), ref _archotech);

						if (tempSearchFilter != _searchFilter ||
						    tempNeolithic != _neolithic ||
						    tempMedieval != _medieval ||
						    tempIndustrial != _industrial ||
						    tempSpacer != _spacer ||
						    tempUltra != _ultra ||
						    tempArcho != _archotech)
						{
							_minorChangesMade = true;
						}

						Widgets.DrawMenuSection(weaponsInRect);
						var weaponsOutRect = new Rect(weaponsInRect.ContractedBy(Margin));
						float num2 = ((AmmoLogic.AvailableProjectileWeapons.Count(x =>
							               (x.techLevel == TechLevel.Neolithic && _neolithic) ||
							               (x.techLevel == TechLevel.Medieval && _medieval) ||
							               (x.techLevel == TechLevel.Industrial && _industrial) ||
							               (x.techLevel == TechLevel.Spacer && _spacer) ||
							               (x.techLevel == TechLevel.Ultra && _ultra) ||
							               (x.techLevel == TechLevel.Archotech && _archotech)) *
						               (_lineHeight)));

						if (num2 < weaponsOutRect.height)
						{
							num2 = weaponsOutRect.height;
						}

						//Weapons
						var weaponsViewRect = new Rect(0f, 0f, weaponsOutRect.width - Margin * 4, num2);
						Widgets.BeginScrollView(weaponsOutRect, ref _scrollWeaponsPos, weaponsViewRect);
						var weaponsList = new Listing_Standard(weaponsInRect, () => _scrollWeaponsPos);
						weaponsList.Begin(weaponsViewRect);
						foreach (ThingDef thingDef in AmmoLogic.AvailableProjectileWeapons.Where(thingDef =>
							         thingDef.label.ToLower().Contains(_searchFilter.ToLower())))
						{
							switch (thingDef.techLevel)
							{
								case TechLevel.Neolithic:
									if (!_neolithic)
									{
										continue;
									}

									break;


								case TechLevel.Medieval:
									if (!_medieval)
									{
										continue;
									}

									break;


								case TechLevel.Industrial:
									if (!_industrial)
									{
										continue;
									}

									break;


								case TechLevel.Spacer:
									if (!_spacer)
									{
										continue;
									}

									break;


								case TechLevel.Ultra:
									if (!_ultra)
									{
										continue;
									}

									break;


								case TechLevel.Archotech:
									if (!_archotech)
									{
										continue;
									}

									break;


								case TechLevel.Undefined:
								case TechLevel.Animal:
								default:
									break;
							}

							Rect weaponRect = weaponsList.GetRect(_lineHeight);
							weaponRect.SplitVertically(25,
								out Rect iconRect,
								out Rect mainWeaponRect);

							mainWeaponRect.SplitVertically(mainWeaponRect.width * 0.45f,
								out Rect weaponLabelRect,
								out Rect weaponOptionsRect);

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
							if (_chosenCategory == null)
							{
								return;
							}

							CategoryWeaponDictionary.TryGetValue(_chosenCategory.defName,
								out Dictionary<string, bool> dic);

							dic.TryGetValue(thingDef.defName, out bool res);
							ExemptionWeaponDictionary.TryGetValue(thingDef.defName, out bool exemption);
							bool val = res;
							if (val)
							{
								_filterCatTrue = true;
							}
							else
							{
								_filterCatFalse = true;
							}

							bool val2 = exemption;
							if (val2)
							{
								_filterAmmoFalse = true;
							}
							else
							{
								_filterAmmoTrue = true;
							}

							Text.Font = GameFont.Tiny;
							Text.Anchor = TextAnchor.MiddleRight;

							if (_setAllCatFalse)
							{
								dic.SetOrAdd(thingDef.defName, false);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
							}
							else if (_setAllCatTrue)
							{
								dic.SetOrAdd(thingDef.defName, true);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
							}

							Widgets.Label(categoryUsageLabelRect,
								Translate.AmmoCategoryOptions(_chosenCategory?.LabelCap));

							if (Widgets.ButtonImage(categoryUsageIconRect, val ? TrueIcon : FalseIcon))
							{
								val = !val;
								dic.SetOrAdd(thingDef.defName, val);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
								_changesMade = true;
							}

							if (_setAllAmmoFalse)
							{
								ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, true);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
							}
							else if (_setAllAmmoTrue)
							{
								ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, false);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
							}

							Widgets.Label(ammoUsageLabelRect, Translate.AmmoUsageOptions);
							if (Widgets.ButtonImage(ammoUsageIconRect, val2 ? FalseIcon : TrueIcon))
							{
								val2 = !val2;
								ExemptionWeaponDictionary.SetOrAdd(thingDef.defName, val2);
								AmmoLogic.ResetHyperLinksForWeapon(thingDef);
								_changesMade = true;
							}

							Text.Font = GameFont.Small;
							Text.Anchor = TextAnchor.UpperLeft;
						}

						weaponsList.End();
						Widgets.EndScrollView();
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
					_setAllAmmoTrue = false;
					_setAllAmmoFalse = false;
					_setAllCatTrue = false;
					_setAllCatFalse = false;
					_minorChangesMade = false;
				}

				if (_changesMade)
				{
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
	}
}